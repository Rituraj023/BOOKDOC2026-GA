using System.Security.Claims;
using BookDoc2026.Application;
using BookDoc2026.Application.Abstractions;
using BookDoc2026.Application.Foundation;
using BookDoc2026.Contracts.Foundation;
using BookDoc2026.Contracts.Security;
using BookDoc2026.Domain.Common;
using BookDoc2026.Domain.Communications;
using BookDoc2026.Domain.Foundation;
using BookDoc2026.Domain.Identity;
using BookDoc2026.Infrastructure;
using BookDoc2026.Infrastructure.Data;
using BookDoc2026.Infrastructure.Messaging;
using BookDoc2026.Messaging;
using BookDoc2026.Templates;
using BookDoc2026.Shared.Kernel.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BookDoc2026.IntegrationTests;

public sealed class FoundationReferenceSliceTests
{
    [Fact]
    public async Task ApprovedApplication_CreatesIsolatedTenantGraphAuditAndOutbox()
    {
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var execution = scope.ServiceProvider.GetRequiredService<TestExecutionContext>();
        var service = scope.ServiceProvider.GetRequiredService<FoundationService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<BookDocDbContext>();
        var publicIds = scope.ServiceProvider.GetRequiredService<IPublicIdCodec>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        execution.BecomePlatform(
            FoundationPermissions.TenantsRegister,
            FoundationPermissions.TenantsApprove);
        var application = await service.SubmitApplicationAsync(NewApplication("clinic-a"), "PlatformOperator", default);
        var provisioned = await service.ApproveApplicationAsync(
            publicIds.Decode(PublicIdKind.TenantApplication, application.Id),
            new ApproveTenantApplicationRequest(application.Version),
            default);

        Assert.Equal(1, await dbContext.Tenants.CountAsync());
        Assert.Equal(1, await dbContext.Organizations.IgnoreQueryFilters().CountAsync());
        Assert.Equal(1, await dbContext.Branches.IgnoreQueryFilters().CountAsync());
        Assert.Equal(1, await dbContext.BranchConfigurations.IgnoreQueryFilters().CountAsync());
        Assert.Equal(2, await dbContext.AuditEvents.CountAsync());
        Assert.Equal(1, await dbContext.OutboxMessages.CountAsync());
        Assert.Equal(1, await dbContext.MessageTemplates.IgnoreQueryFilters().CountAsync());

        var tenant = TestPublicIds.DecodeTenant(scope.ServiceProvider, provisioned);
        var role = await roleManager.FindByNameAsync(BookDocRoleNames.ClinicAdministrator);
        if (role is null)
        {
            role = new ApplicationRole
            {
                Name = BookDocRoleNames.ClinicAdministrator,
                IsActive = true
            };
            Assert.True((await roleManager.CreateAsync(role)).Succeeded);
            Assert.True((await roleManager.AddClaimAsync(
                role,
                new Claim(BookDocClaimTypes.Permission, FoundationPermissions.BranchesView))).Succeeded);
            Assert.True((await roleManager.AddClaimAsync(
                role,
                new Claim(BookDocClaimTypes.Permission, FoundationPermissions.BranchesConfigurationManage))).Succeeded);
        }
        var identityUser = new ApplicationUser
        {
            UserName = "branch.admin@example.invalid",
            Email = "branch.admin@example.invalid",
            DisplayName = "Delhi Branch Administrator"
        };
        Assert.True((await userManager.CreateAsync(identityUser)).Succeeded);
        execution.BecomeTenant(
            tenant.TenantId,
            [tenant.BranchId],
            FoundationPermissions.BranchesView,
            FoundationPermissions.BranchesConfigurationManage,
            FoundationPermissions.BranchAdministratorsManage);
        var administrator = await service.CreateBranchAdministratorAsync(
            tenant.BranchId,
            new CreateBranchAdministratorRequest(
                publicIds.Encode(PublicIdKind.IdentitySubject, identityUser.Id)),
            default);
        var configuration = await service.UpdateBranchConfigurationAsync(
            tenant.BranchId,
            new UpdateBranchConfigurationRequest(
                1,
                null,
                "CLA-INV",
                "CLA-RCT",
                "sender@example.invalid",
                null),
            default);

        Assert.Equal(2, configuration.Version);
        Assert.Equal("ClinicAdministrator", administrator.RoleCode);
        Assert.Equal(nameof(CommunicationVerificationStatus.Pending), configuration.CommunicationVerificationStatus);
        Assert.Equal(1, await dbContext.UserScopes.CountAsync());
        Assert.True(await userManager.IsInRoleAsync(identityUser, BookDocRoleNames.ClinicAdministrator));
        Assert.Equal(4, await dbContext.AuditEvents.CountAsync());
    }

    [Fact]
    public async Task TenantQueryFilter_BlocksCrossTenantBranchEvenWhenBranchClaimIsForged()
    {
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var execution = scope.ServiceProvider.GetRequiredService<TestExecutionContext>();
        var service = scope.ServiceProvider.GetRequiredService<FoundationService>();
        var publicIds = scope.ServiceProvider.GetRequiredService<IPublicIdCodec>();

        execution.BecomePlatform(FoundationPermissions.TenantsApprove);
        var first = await service.SubmitApplicationAsync(NewApplication("clinic-first"), "ClinicApplicant", default);
        var firstTenantResponse = await service.ApproveApplicationAsync(publicIds.Decode(PublicIdKind.TenantApplication, first.Id), new(first.Version), default);
        var second = await service.SubmitApplicationAsync(NewApplication("clinic-second"), "ClinicApplicant", default);
        var secondTenantResponse = await service.ApproveApplicationAsync(publicIds.Decode(PublicIdKind.TenantApplication, second.Id), new(second.Version), default);
        var firstTenant = TestPublicIds.DecodeTenant(scope.ServiceProvider, firstTenantResponse);
        var secondTenant = TestPublicIds.DecodeTenant(scope.ServiceProvider, secondTenantResponse);

        execution.BecomeTenant(
            firstTenant.TenantId,
            [secondTenant.BranchId],
            FoundationPermissions.BranchesView);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.GetBranchConfigurationAsync(secondTenant.BranchId, default));
    }

    [Fact]
    public async Task BranchConfiguration_RequiresPermissionAndRejectsStaleVersion()
    {
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var execution = scope.ServiceProvider.GetRequiredService<TestExecutionContext>();
        var service = scope.ServiceProvider.GetRequiredService<FoundationService>();
        var publicIds = scope.ServiceProvider.GetRequiredService<IPublicIdCodec>();

        execution.BecomePlatform(FoundationPermissions.TenantsApprove);
        var application = await service.SubmitApplicationAsync(NewApplication("clinic-version"), "ClinicApplicant", default);
        var tenantResponse = await service.ApproveApplicationAsync(publicIds.Decode(PublicIdKind.TenantApplication, application.Id), new(application.Version), default);
        var tenant = TestPublicIds.DecodeTenant(scope.ServiceProvider, tenantResponse);

        execution.BecomeTenant(tenant.TenantId, [tenant.BranchId], FoundationPermissions.BranchesView);
        await Assert.ThrowsAsync<ForbiddenException>(() => service.UpdateBranchConfigurationAsync(
            tenant.BranchId,
            NewConfiguration(1),
            default));

        execution.BecomeTenant(
            tenant.TenantId,
            [tenant.BranchId],
            FoundationPermissions.BranchesView,
            FoundationPermissions.BranchesConfigurationManage);
        await service.UpdateBranchConfigurationAsync(tenant.BranchId, NewConfiguration(1), default);
        await Assert.ThrowsAsync<ConcurrencyConflictException>(() =>
            service.UpdateBranchConfigurationAsync(tenant.BranchId, NewConfiguration(1), default));
    }

    [Fact]
    public async Task WorkerCompletesOutboxOnce()
    {
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var execution = scope.ServiceProvider.GetRequiredService<TestExecutionContext>();
        var service = scope.ServiceProvider.GetRequiredService<FoundationService>();
        var processor = scope.ServiceProvider.GetRequiredService<IOutboxProcessor>();
        var dbContext = scope.ServiceProvider.GetRequiredService<BookDocDbContext>();
        var publicIds = scope.ServiceProvider.GetRequiredService<IPublicIdCodec>();

        execution.BecomePlatform(FoundationPermissions.TenantsApprove);
        var application = await service.SubmitApplicationAsync(NewApplication("clinic-worker"), "ClinicApplicant", default);
        await service.ApproveApplicationAsync(publicIds.Decode(PublicIdKind.TenantApplication, application.Id), new(application.Version), default);

        Assert.Equal(1, await processor.ProcessBatchAsync(default));
        Assert.Equal(0, await processor.ProcessBatchAsync(default));

        var outbox = await dbContext.OutboxMessages.SingleAsync();
        Assert.Equal(OutboxStatus.Completed, outbox.Status);
        Assert.Equal(1, outbox.AttemptCount);
        var delivery = await dbContext.MessageDeliveryAttempts.IgnoreQueryFilters().SingleAsync();
        Assert.Equal("development-email", delivery.ProviderCode);
        Assert.Equal(MessageDeliveryStatus.Accepted, delivery.Status);
        Assert.DoesNotContain("clinic-worker@", delivery.RecipientHint, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TemplateCatalog_PrefersBranchScopeAndNeverReadsAnotherTenant()
    {
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var execution = scope.ServiceProvider.GetRequiredService<TestExecutionContext>();
        var dbContext = scope.ServiceProvider.GetRequiredService<BookDocDbContext>();
        var catalog = scope.ServiceProvider.GetRequiredService<ITemplateCatalog>();
        execution.BecomePlatform();
        var tenantId = NumericId.Next();
        var otherTenantId = NumericId.Next();
        var organizationId = NumericId.Next();
        var branchId = NumericId.Next();

        var tenantTemplate = PublishedTemplate(tenantId, null, null, "tenant");
        var branchTemplate = PublishedTemplate(tenantId, organizationId, branchId, "branch");
        var otherTemplate = PublishedTemplate(otherTenantId, null, null, "other-tenant");
        dbContext.MessageTemplates.AddRange(tenantTemplate, branchTemplate, otherTemplate);
        await dbContext.SaveChangesAsync();

        var selected = await catalog.ResolvePublishedAsync(new TemplateSelection(
            new TemplateScope(tenantId, organizationId, branchId),
            "Booking.Confirmed.Patient",
            TemplateChannel.Email,
            "en-IN"));
        var otherSelected = await catalog.ResolvePublishedAsync(new TemplateSelection(
            new TemplateScope(otherTenantId),
            "Booking.Confirmed.Patient",
            TemplateChannel.Email,
            "en-IN"));

        Assert.NotNull(selected);
        Assert.Equal("branch", selected!.BodyTemplate);
        Assert.NotNull(otherSelected);
        Assert.Equal("other-tenant", otherSelected!.BodyTemplate);
    }

    [Fact]
    public async Task PermanentProviderFailure_IsRecordedAndDeadLetteredWithoutRawRecipient()
    {
        var messageProvider = new SequencedEmailProvider(
            MessageDeliveryResult.Failed("provider_rejected", transient: false));
        await using var provider = BuildProvider(messageProvider);
        using var scope = provider.CreateScope();
        var execution = scope.ServiceProvider.GetRequiredService<TestExecutionContext>();
        var service = scope.ServiceProvider.GetRequiredService<FoundationService>();
        var processor = scope.ServiceProvider.GetRequiredService<IOutboxProcessor>();
        var dbContext = scope.ServiceProvider.GetRequiredService<BookDocDbContext>();
        var publicIds = scope.ServiceProvider.GetRequiredService<IPublicIdCodec>();

        execution.BecomePlatform(FoundationPermissions.TenantsApprove);
        var application = await service.SubmitApplicationAsync(NewApplication("clinic-rejected"), "ClinicApplicant", default);
        await service.ApproveApplicationAsync(
            publicIds.Decode(PublicIdKind.TenantApplication, application.Id),
            new(application.Version),
            default);

        Assert.Equal(0, await processor.ProcessBatchAsync(default));
        var outbox = await dbContext.OutboxMessages.SingleAsync();
        var attempt = await dbContext.MessageDeliveryAttempts.IgnoreQueryFilters().SingleAsync();
        Assert.Equal(OutboxStatus.DeadLetter, outbox.Status);
        Assert.Equal("provider_rejected", outbox.LastErrorCode);
        Assert.Equal(MessageDeliveryStatus.PermanentFailure, attempt.Status);
        Assert.DoesNotContain("clinic-rejected@", attempt.RecipientHint, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TransientProviderFailure_RetriesWithSameOperationAndCompletesOnce()
    {
        var time = new AdjustableTimeProvider(new DateTimeOffset(2026, 8, 18, 9, 0, 0, TimeSpan.Zero));
        var messageProvider = new SequencedEmailProvider(
            MessageDeliveryResult.Failed("provider_busy", transient: true),
            MessageDeliveryResult.Sent("accepted-on-retry"));
        await using var provider = BuildProvider(messageProvider, time);
        using var scope = provider.CreateScope();
        var execution = scope.ServiceProvider.GetRequiredService<TestExecutionContext>();
        var service = scope.ServiceProvider.GetRequiredService<FoundationService>();
        var processor = scope.ServiceProvider.GetRequiredService<IOutboxProcessor>();
        var dbContext = scope.ServiceProvider.GetRequiredService<BookDocDbContext>();
        var publicIds = scope.ServiceProvider.GetRequiredService<IPublicIdCodec>();

        execution.BecomePlatform(FoundationPermissions.TenantsApprove);
        var application = await service.SubmitApplicationAsync(NewApplication("clinic-retry"), "ClinicApplicant", default);
        await service.ApproveApplicationAsync(
            publicIds.Decode(PublicIdKind.TenantApplication, application.Id),
            new(application.Version),
            default);

        Assert.Equal(0, await processor.ProcessBatchAsync(default));
        var outbox = await dbContext.OutboxMessages.SingleAsync();
        Assert.Equal(OutboxStatus.RetryScheduled, outbox.Status);
        var operationId = outbox.OperationId;

        time.Advance(TimeSpan.FromSeconds(3));
        Assert.Equal(1, await processor.ProcessBatchAsync(default));
        Assert.Equal(0, await processor.ProcessBatchAsync(default));

        outbox = await dbContext.OutboxMessages.SingleAsync();
        var attempts = await dbContext.MessageDeliveryAttempts.IgnoreQueryFilters()
            .OrderBy(attempt => attempt.AttemptNumber)
            .ToListAsync();
        Assert.Equal(OutboxStatus.Completed, outbox.Status);
        Assert.Equal(2, outbox.AttemptCount);
        Assert.Equal(2, attempts.Count);
        Assert.All(attempts, attempt => Assert.Equal(operationId, attempt.OperationId));
        Assert.Equal(MessageDeliveryStatus.TransientFailure, attempts[0].Status);
        Assert.Equal(MessageDeliveryStatus.Accepted, attempts[1].Status);
        Assert.Equal(2, messageProvider.SendCount);
    }

    private static ServiceProvider BuildProvider(
        IMessageProvider? messageProvider = null,
        TimeProvider? timeProvider = null)
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Outbox:BatchSize"] = "20",
                ["Outbox:MaxAttempts"] = "5",
                ["Outbox:LeaseDuration"] = "00:02:00",
                ["Outbox:PollInterval"] = "00:00:01"
            })
            .Build();
        var execution = new TestExecutionContext();
        services.AddLogging();
        if (timeProvider is not null)
        {
            services.AddSingleton(timeProvider);
        }
        services.AddSingleton(execution);
        services.AddSingleton<ICurrentActor>(execution);
        services.AddSingleton<ICorrelationContext>(execution);
        services.AddBookDocApplication();
        services.AddBookDocInfrastructureForTesting(
            configuration,
            options => options.UseInMemoryDatabase($"bookdoc-{Guid.NewGuid():N}"));
        services.AddBookDocMessaging();
        services.AddSingleton<IMessageProvider>(messageProvider ?? new DevelopmentEmailMessageProvider());
        return services.BuildServiceProvider(validateScopes: true);
    }

    private static SubmitTenantApplicationRequest NewApplication(string slug) =>
        new($"{slug} Legal Name", slug, $"{slug}@example.invalid", "Delhi Main", "DEL01");

    private static UpdateBranchConfigurationRequest NewConfiguration(long version) =>
        new(version, null, "NEW-INV", "NEW-RCT", null, null);

    private static MessageTemplate PublishedTemplate(
        long tenantId,
        long? organizationId,
        long? branchId,
        string body)
    {
        var template = MessageTemplate.CreateDraft(
            tenantId,
            organizationId,
            branchId,
            "Booking.Confirmed.Patient",
            1,
            CommunicationChannel.Email,
            "en-IN",
            MessageTemplateContentKind.PlainText,
            body,
            null,
            DateTimeOffset.UtcNow);
        template.Publish(template.Revision, DateTimeOffset.UtcNow);
        return template;
    }

    private sealed class SequencedEmailProvider(params MessageDeliveryResult[] results) : IMessageProvider
    {
        private readonly Queue<MessageDeliveryResult> _results = new(results);

        public string ProviderCode => "sequenced-test-email";

        public MessageChannel Channel => MessageChannel.Email;

        public int SendCount { get; private set; }

        public ValueTask<MessageDeliveryResult> SendAsync(
            MessageEnvelope message,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SendCount++;
            return ValueTask.FromResult(_results.Dequeue());
        }
    }

    private sealed class AdjustableTimeProvider(DateTimeOffset current) : TimeProvider
    {
        private DateTimeOffset _current = current;

        public override DateTimeOffset GetUtcNow() => _current;

        public void Advance(TimeSpan duration) => _current = _current.Add(duration);
    }
}
