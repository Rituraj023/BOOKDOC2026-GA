using System.Text.Json;
using BookDoc2026.Application.Abstractions;
using BookDoc2026.Application.Foundation;
using BookDoc2026.Domain.Communications;
using BookDoc2026.Infrastructure.Data;
using BookDoc2026.Messaging;
using BookDoc2026.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BookDoc2026.Infrastructure.Worker;

public sealed class TenantApprovedMessageHandler(
    BookDocDbContext dbContext,
    IMessageDispatcher dispatcher,
    IClock clock,
    ILogger<TenantApprovedMessageHandler> logger) : IOutboxMessageHandler
{
    public string MessageType => "Foundation.TenantApproved.v1";

    public async Task HandleAsync(OutboxMessageContext context, CancellationToken cancellationToken)
    {
        TenantApprovedOutboxPayload payload;
        try
        {
            payload = JsonSerializer.Deserialize<TenantApprovedOutboxPayload>(context.PayloadJson)
                ?? throw new JsonException("The payload is empty.");
        }
        catch (JsonException)
        {
            throw new OutboxDispatchException("invalid_message_payload", false, "The message payload is invalid.");
        }

        if (context.TenantId != payload.TenantId || context.BranchId != payload.BranchId)
        {
            throw new OutboxDispatchException("message_scope_mismatch", false, "The message scope is invalid.");
        }

        if (await dbContext.MessageDeliveryAttempts
            .IgnoreQueryFilters()
            .AnyAsync(attempt =>
                attempt.TenantId == payload.TenantId
                && attempt.OperationId == context.OperationId
                && attempt.Channel == CommunicationChannel.Email
                && attempt.Status == MessageDeliveryStatus.Accepted,
                cancellationToken))
        {
            return;
        }

        var application = await dbContext.TenantApplications
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == payload.TenantApplicationId, cancellationToken)
            ?? throw new OutboxDispatchException("tenant_application_not_found", false, "The message source was not found.");

        MessageDeliveryResult result;
        try
        {
            result = await dispatcher.DispatchAsync(
                new MessageDispatchRequest(
                    new TemplateScope(payload.TenantId, payload.OrganizationId, payload.BranchId),
                    MessageChannel.Email,
                    "Tenant.Approved.Contact",
                    "en-IN",
                    new MessageRecipient(application.ContactEmail, application.LegalName),
                    new Dictionary<string, string?> { ["ClinicName"] = application.LegalName },
                    context.OperationId),
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (MessageConfigurationException exception)
        {
            logger.LogWarning(
                "Tenant-approved message configuration failed. ErrorCode={ErrorCode} CorrelationId={CorrelationId}",
                exception.ErrorCode,
                context.CorrelationId);
            throw new OutboxDispatchException(
                exception.ErrorCode,
                false,
                "The message configuration is invalid.");
        }
        catch (TemplateRenderException)
        {
            throw new OutboxDispatchException(
                "message_template_render_failed",
                false,
                "The message template could not be rendered.");
        }
        catch (ArgumentException)
        {
            throw new OutboxDispatchException(
                "message_request_invalid",
                false,
                "The message request is invalid.");
        }

        var status = result.Accepted
            ? MessageDeliveryStatus.Accepted
            : result.IsTransientFailure
                ? MessageDeliveryStatus.TransientFailure
                : MessageDeliveryStatus.PermanentFailure;
        var attempt = MessageDeliveryAttempt.Record(
            payload.TenantId,
            payload.BranchId,
            context.MessageId,
            context.OperationId,
            context.AttemptNumber,
            CommunicationChannel.Email,
            "Tenant.Approved.Contact",
            result.TemplateVersion ?? 1,
            result.ProviderCode ?? "unavailable",
            MaskEmail(application.ContactEmail),
            status,
            result.ProviderMessageId,
            result.ErrorCode,
            clock.UtcNow);
        await dbContext.MessageDeliveryAttempts.AddAsync(attempt, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (!result.Accepted)
        {
            throw new OutboxDispatchException(
                result.ErrorCode ?? "message_delivery_failed",
                result.IsTransientFailure,
                "The message provider did not accept the delivery.");
        }
    }

    private static string MaskEmail(string address)
    {
        var separator = address.LastIndexOf('@');
        return separator > 0 && separator < address.Length - 1
            ? $"{address[0]}***{address[separator..]}"
            : "***";
    }
}
