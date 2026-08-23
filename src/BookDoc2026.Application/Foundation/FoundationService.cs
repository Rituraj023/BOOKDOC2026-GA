using System.Text.Json;
using BookDoc2026.Application.Abstractions;
using BookDoc2026.Application.Scheduling;
using BookDoc2026.Contracts.Foundation;
using BookDoc2026.Contracts.Security;
using BookDoc2026.Domain.Common;
using BookDoc2026.Domain.Communications;
using BookDoc2026.Domain.Foundation;

namespace BookDoc2026.Application.Foundation;

public sealed class FoundationService(
    IFoundationRepository repository,
    ICurrentActor actor,
    IPublicIdCodec publicIds,
    IIdentityAdministrationService identityAdministration,
    IClock clock,
    ICorrelationContext correlationContext)
{
    public async Task<TenantApplicationResponse> SubmitApplicationAsync(
        SubmitTenantApplicationRequest request,
        string submissionSource,
        CancellationToken cancellationToken)
    {
        if (string.Equals(submissionSource, "PlatformOperator", StringComparison.Ordinal))
        {
            RequirePlatformPermission(FoundationPermissions.TenantsRegister);
        }

        if (await repository.TenantSlugExistsAsync(request.Slug.Trim().ToLowerInvariant(), cancellationToken))
        {
            throw new DomainRuleException("A clinic tenant already uses this slug.");
        }

        var now = clock.UtcNow;
        var application = TenantApplication.Submit(
            request.LegalName,
            request.Slug,
            request.ContactEmail,
            request.FirstBranchName,
            request.FirstBranchCode,
            submissionSource,
            now);

        await repository.AddTenantApplicationAsync(application, cancellationToken);
        await repository.AddAuditEventAsync(AuditEvent.Record(
            null,
            null,
            actor.ActorId,
            "TenantApplication.Submitted",
            nameof(TenantApplication),
            application.Id,
            JsonSerializer.Serialize(new { application.Slug, application.SubmissionSource }),
            correlationContext.CorrelationId,
            now), cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return Map(application);
    }

    public async Task<TenantProvisioningResponse> ApproveApplicationAsync(
        long applicationId,
        ApproveTenantApplicationRequest request,
        CancellationToken cancellationToken)
    {
        RequirePlatformPermission(FoundationPermissions.TenantsApprove);
        var application = await repository.GetTenantApplicationAsync(applicationId, cancellationToken)
            ?? throw new NotFoundException("Clinic application was not found.");

        if (await repository.TenantSlugExistsAsync(application.Slug, cancellationToken))
        {
            throw new DomainRuleException("A clinic tenant already uses this slug.");
        }

        var now = clock.UtcNow;
        application.Approve(actor.ActorId, request.ExpectedVersion, now);
        var tenant = Tenant.Activate(application.LegalName, application.Slug, now);
        var organization = Organization.Create(tenant.Id, application.LegalName, now);
        var branch = Branch.Create(
            tenant.Id,
            organization.Id,
            application.FirstBranchCode,
            application.FirstBranchName,
            now);
        var configuration = BranchConfiguration.CreateDefault(tenant.Id, branch.Id, branch.Code, now);
        var approvalTemplate = MessageTemplate.CreateDraft(
            tenant.Id,
            null,
            null,
            "Tenant.Approved.Contact",
            1,
            CommunicationChannel.Email,
            "en-IN",
            MessageTemplateContentKind.Html,
            "<p>Hello {{ClinicName}}, your BOOKDOC2026 clinic account is approved.</p>",
            "Your clinic account is approved",
            now);
        approvalTemplate.Publish(approvalTemplate.Revision, now);
        var bookingTemplate = MessageTemplate.CreateDraft(
            tenant.Id,
            null,
            null,
            BookingConfirmedOutboxPayload.TemplateKey,
            1,
            CommunicationChannel.Email,
            "en-IN",
            MessageTemplateContentKind.Html,
            "<p>Hello {{PatientName}}, booking {{BookingNumber}} for {{ServiceName}} is confirmed for {{StartTime}} at {{ClinicName}}.</p>",
            "Booking {{BookingNumber}} confirmed",
            now);
        bookingTemplate.Publish(bookingTemplate.Revision, now);
        var bookingCancelledTemplate = MessageTemplate.CreateDraft(
            tenant.Id, null, null, BookingLifecycleOutboxPayload.CancelledTemplateKey, 1,
            CommunicationChannel.Email, "en-IN", MessageTemplateContentKind.Html,
            "<p>Hello {{PatientName}}, booking {{BookingNumber}} for {{ServiceName}} at {{ClinicName}} was cancelled. Reason: {{Reason}}.</p>",
            "Booking {{BookingNumber}} cancelled", now);
        bookingCancelledTemplate.Publish(bookingCancelledTemplate.Revision, now);
        var bookingRescheduledTemplate = MessageTemplate.CreateDraft(
            tenant.Id, null, null, BookingLifecycleOutboxPayload.RescheduledTemplateKey, 1,
            CommunicationChannel.Email, "en-IN", MessageTemplateContentKind.Html,
            "<p>Hello {{PatientName}}, booking {{PreviousBookingNumber}} was moved from {{PreviousStartTime}} to {{StartTime}}. New booking: {{BookingNumber}} for {{ServiceName}} at {{ClinicName}}. Reason: {{Reason}}.</p>",
            "Booking rescheduled to {{StartTime}}", now);
        bookingRescheduledTemplate.Publish(bookingRescheduledTemplate.Revision, now);
        var waitlistPromotedTemplate = MessageTemplate.CreateDraft(
            tenant.Id, null, null, BookingLifecycleOutboxPayload.WaitlistPromotedTemplateKey, 1,
            CommunicationChannel.Email, "en-IN", MessageTemplateContentKind.Html,
            "<p>Hello {{PatientName}}, your waitlist request is confirmed as booking {{BookingNumber}} for {{ServiceName}} on {{StartTime}} at {{ClinicName}}.</p>",
            "Waitlist booking {{BookingNumber}} confirmed", now);
        waitlistPromotedTemplate.Publish(waitlistPromotedTemplate.Revision, now);
        var outbox = OutboxMessage.Enqueue(
            tenant.Id,
            branch.Id,
            "Foundation.TenantApproved.v1",
            JsonSerializer.Serialize(new TenantApprovedOutboxPayload(application.Id, tenant.Id, organization.Id, branch.Id)),
            now,
            Guid.NewGuid(),
            correlationContext.CorrelationId);

        await repository.AddTenantAsync(tenant, cancellationToken);
        await repository.AddOrganizationAsync(organization, cancellationToken);
        await repository.AddBranchAsync(branch, cancellationToken);
        await repository.AddBranchConfigurationAsync(configuration, cancellationToken);
        await repository.AddMessageTemplateAsync(approvalTemplate, cancellationToken);
        await repository.AddMessageTemplateAsync(bookingTemplate, cancellationToken);
        await repository.AddMessageTemplateAsync(bookingCancelledTemplate, cancellationToken);
        await repository.AddMessageTemplateAsync(bookingRescheduledTemplate, cancellationToken);
        await repository.AddMessageTemplateAsync(waitlistPromotedTemplate, cancellationToken);
        await repository.AddOutboxMessageAsync(outbox, cancellationToken);
        await repository.AddAuditEventAsync(AuditEvent.Record(
            tenant.Id,
            branch.Id,
            actor.ActorId,
            "TenantApplication.Approved",
            nameof(TenantApplication),
            application.Id,
            JsonSerializer.Serialize(new { tenant.Id, OrganizationId = organization.Id, BranchId = branch.Id }),
            correlationContext.CorrelationId,
            now), cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return new TenantProvisioningResponse(
            publicIds.Encode(PublicIdKind.Tenant, tenant.Id),
            publicIds.Encode(PublicIdKind.Organization, organization.Id, tenant.Id),
            publicIds.Encode(PublicIdKind.Branch, branch.Id, tenant.Id),
            publicIds.Encode(PublicIdKind.BranchConfiguration, configuration.Id, tenant.Id),
            publicIds.Encode(PublicIdKind.OutboxMessage, outbox.Id, tenant.Id));
    }

    public async Task<BranchConfigurationResponse> GetBranchConfigurationAsync(
        long branchId,
        CancellationToken cancellationToken)
    {
        RequireBranchPermission(FoundationPermissions.BranchesView, branchId);
        var result = await repository.GetBranchConfigurationAsync(branchId, cancellationToken)
            ?? throw new NotFoundException("Branch was not found in the current tenant scope.");
        return Map(result.Branch, result.Configuration);
    }

    public async Task<BranchAdministratorResponse> CreateBranchAdministratorAsync(
        long branchId,
        CreateBranchAdministratorRequest request,
        CancellationToken cancellationToken)
    {
        RequireBranchPermission(FoundationPermissions.BranchAdministratorsManage, branchId);
        var branchResult = await repository.GetBranchConfigurationAsync(branchId, cancellationToken)
            ?? throw new NotFoundException("Branch was not found in the current tenant scope.");

        var subjectId = publicIds.Decode(PublicIdKind.IdentitySubject, request.SubjectId);
        var now = clock.UtcNow;
        var assignment = await identityAdministration.AssignClinicAdministratorAsync(
            subjectId,
            branchResult.Branch.TenantId,
            branchResult.Branch.OrganizationId,
            branchId,
            cancellationToken);
        await repository.AddAuditEventAsync(AuditEvent.Record(
            branchResult.Branch.TenantId,
            branchId,
            actor.ActorId,
            "BranchAdministrator.Created",
            "ApplicationUserScope",
            assignment.ScopeId,
            JsonSerializer.Serialize(new { assignment.SubjectId, RoleCode = "ClinicAdministrator" }),
            correlationContext.CorrelationId,
            now), cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return new BranchAdministratorResponse(
            publicIds.Encode(PublicIdKind.UserScope, assignment.ScopeId, branchResult.Branch.TenantId),
            publicIds.Encode(PublicIdKind.IdentitySubject, assignment.SubjectId),
            publicIds.Encode(PublicIdKind.Branch, branchId, branchResult.Branch.TenantId),
            assignment.DisplayName,
            assignment.Email,
            "ClinicAdministrator",
            assignment.Permissions);
    }

    public async Task<BranchConfigurationResponse> UpdateBranchConfigurationAsync(
        long branchId,
        UpdateBranchConfigurationRequest request,
        CancellationToken cancellationToken)
    {
        RequireBranchPermission(FoundationPermissions.BranchesConfigurationManage, branchId);
        var result = await repository.GetBranchConfigurationAsync(branchId, cancellationToken)
            ?? throw new NotFoundException("Branch was not found in the current tenant scope.");

        if (await repository.NumberPrefixInUseAsync(
                result.Branch.TenantId,
                branchId,
                request.InvoicePrefix.Trim().ToUpperInvariant(),
                request.ReceiptPrefix.Trim().ToUpperInvariant(),
                cancellationToken))
        {
            throw new DomainRuleException("Invoice or receipt prefix is already used by another branch.");
        }

        var oldVersion = result.Configuration.Version;
        var now = clock.UtcNow;
        result.Configuration.Update(
            request.ExpectedVersion,
            request.LogoUrl,
            request.InvoicePrefix,
            request.ReceiptPrefix,
            request.EmailSender,
            request.WhatsAppNumber,
            now);

        await repository.AddAuditEventAsync(AuditEvent.Record(
            result.Branch.TenantId,
            branchId,
            actor.ActorId,
            "BranchConfiguration.Updated",
            nameof(BranchConfiguration),
            result.Configuration.Id,
            JsonSerializer.Serialize(new
            {
                OldVersion = oldVersion,
                NewVersion = result.Configuration.Version,
                result.Configuration.InvoicePrefix,
                result.Configuration.ReceiptPrefix,
                result.Configuration.CommunicationVerificationStatus
            }),
            correlationContext.CorrelationId,
            now), cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return Map(result.Branch, result.Configuration);
    }

    private void RequirePlatformPermission(string permission)
    {
        if (!actor.IsPlatformOperator || !actor.HasPermission(permission))
        {
            throw new ForbiddenException("A platform-operator permission is required.");
        }
    }

    private void RequireBranchPermission(string permission, long branchId)
    {
        if (actor.TenantId is null || !actor.BranchIds.Contains(branchId) || !actor.HasPermission(permission))
        {
            throw new ForbiddenException("The actor is not authorized for this branch operation.");
        }
    }

    private TenantApplicationResponse Map(TenantApplication application) =>
        new(publicIds.Encode(PublicIdKind.TenantApplication, application.Id), application.LegalName, application.Slug, application.Status.ToString(), application.Version);

    private BranchConfigurationResponse Map(Branch branch, BranchConfiguration configuration) =>
        new(
            publicIds.Encode(PublicIdKind.Branch, branch.Id, branch.TenantId),
            branch.Code,
            branch.Name,
            configuration.LogoUrl,
            configuration.InvoicePrefix,
            configuration.ReceiptPrefix,
            configuration.EmailSender,
            configuration.WhatsAppNumber,
            configuration.CommunicationVerificationStatus.ToString(),
            configuration.Version);
}
