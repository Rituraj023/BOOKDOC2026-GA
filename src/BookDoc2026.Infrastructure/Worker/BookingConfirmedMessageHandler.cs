using System.Text.Json;
using BookDoc2026.Application.Abstractions;
using BookDoc2026.Application.Communications;
using BookDoc2026.Application.Scheduling;
using BookDoc2026.Domain.Communications;
using BookDoc2026.Domain.Stakeholders;
using BookDoc2026.Infrastructure.Data;
using BookDoc2026.Messaging;
using BookDoc2026.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BookDoc2026.Infrastructure.Worker;

public sealed class BookingConfirmedMessageHandler(
    BookDocDbContext dbContext,
    IMessageDispatcher dispatcher,
    IClock clock,
    ILogger<BookingConfirmedMessageHandler> logger) : IOutboxMessageHandler
{
    public string MessageType => BookingConfirmedOutboxPayload.MessageType;

    public async Task HandleAsync(OutboxMessageContext context, CancellationToken cancellationToken)
    {
        BookingConfirmedOutboxPayload payload;
        try
        {
            payload = JsonSerializer.Deserialize<BookingConfirmedOutboxPayload>(context.PayloadJson)
                ?? throw new JsonException();
        }
        catch (JsonException)
        {
            throw new OutboxDispatchException("invalid_booking_message_payload", false, "The booking message payload is invalid.");
        }

        if (context.TenantId != payload.TenantId || context.BranchId != payload.BranchId)
            throw new OutboxDispatchException("booking_message_scope_mismatch", false, "The booking message scope is invalid.");
        if (await dbContext.MessageDeliveryAttempts
            .IgnoreQueryFilters()
            .AnyAsync(attempt => attempt.TenantId == payload.TenantId
                && attempt.OperationId == context.OperationId
                && attempt.Channel == CommunicationChannel.Email
                && (attempt.Status == MessageDeliveryStatus.Accepted
                    || attempt.Status == MessageDeliveryStatus.Suppressed), cancellationToken))
            return;

        var booking = await dbContext.Bookings
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == payload.BookingId
                && candidate.TenantId == payload.TenantId, cancellationToken)
            ?? throw new OutboxDispatchException("booking_not_found", false, "The booking message source was not found.");
        if (booking.BranchId != payload.BranchId
            || booking.PatientId != payload.PatientId)
            throw new OutboxDispatchException("booking_message_source_mismatch", false, "The booking message source is invalid.");

        var latestPreference = await dbContext.CommunicationPreferenceEvents
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(preference => preference.TenantId == payload.TenantId
                && preference.BranchId == payload.BranchId
                && preference.StakeholderId == payload.StakeholderId
                && preference.PurposeCode == BookingConfirmedOutboxPayload.PreferencePurpose
                && preference.MessageClass == CommunicationMessageClass.Transactional
                && preference.Channel == CommunicationChannel.Email)
            .OrderByDescending(preference => preference.CreatedUtc)
            .ThenByDescending(preference => preference.Id)
            .FirstOrDefaultAsync(cancellationToken);
        var preference = CommunicationPreferenceEvaluator.Evaluate(latestPreference, clock.UtcNow);
        if (!preference.CanSend)
        {
            await RecordAttemptAsync(
                context,
                payload,
                "policy",
                "***",
                MessageDeliveryStatus.Suppressed,
                null,
                preference.Code,
                1,
                cancellationToken);
            return;
        }

        var contact = await dbContext.StakeholderContactPoints
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(candidate => candidate.TenantId == payload.TenantId
                && candidate.StakeholderId == payload.StakeholderId
                && candidate.Type == ContactPointType.Email)
            .OrderByDescending(candidate => candidate.IsPrimary)
            .ThenBy(candidate => candidate.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (contact is null)
        {
            await RecordAttemptAsync(
                context,
                payload,
                "unavailable",
                "***",
                MessageDeliveryStatus.PermanentFailure,
                null,
                "recipient_email_missing",
                1,
                cancellationToken);
            throw new OutboxDispatchException("recipient_email_missing", false, "The booking recipient is unavailable.");
        }

        var person = await dbContext.StakeholderPersons
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.TenantId == payload.TenantId
                && candidate.StakeholderId == payload.StakeholderId, cancellationToken)
            ?? throw new OutboxDispatchException("booking_recipient_not_found", false, "The booking recipient was not found.");
        var branch = await dbContext.Branches
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == payload.BranchId
                && candidate.TenantId == payload.TenantId, cancellationToken)
            ?? throw new OutboxDispatchException("booking_branch_not_found", false, "The booking branch was not found.");
        var service = await dbContext.ClinicalServices
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == booking.ServiceId
                && candidate.TenantId == payload.TenantId, cancellationToken)
            ?? throw new OutboxDispatchException("booking_service_not_found", false, "The booking service was not found.");

        string localStart;
        try
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(branch.TimeZoneId);
            localStart = TimeZoneInfo.ConvertTime(booking.StartUtc, timeZone).ToString("dd MMM yyyy, hh:mm tt");
        }
        catch (Exception exception) when (exception is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            throw new OutboxDispatchException("branch_timezone_invalid", false, "The booking branch time zone is invalid.");
        }

        MessageDeliveryResult result;
        try
        {
            result = await dispatcher.DispatchAsync(
                new MessageDispatchRequest(
                    new TemplateScope(payload.TenantId, payload.OrganizationId, payload.BranchId),
                    MessageChannel.Email,
                    BookingConfirmedOutboxPayload.TemplateKey,
                    "en-IN",
                    new MessageRecipient(contact.Value, person.DisplayName),
                    new Dictionary<string, string?>
                    {
                        ["PatientName"] = person.DisplayName,
                        ["BookingNumber"] = booking.BookingNumber,
                        ["ServiceName"] = service.Name,
                        ["StartTime"] = localStart,
                        ["ClinicName"] = branch.Name
                    },
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
                "Booking message configuration failed. ErrorCode={ErrorCode} CorrelationId={CorrelationId}",
                exception.ErrorCode,
                context.CorrelationId);
            throw new OutboxDispatchException(exception.ErrorCode, false, "The booking message configuration is invalid.");
        }
        catch (TemplateRenderException)
        {
            throw new OutboxDispatchException("message_template_render_failed", false, "The booking template could not be rendered.");
        }
        catch (ArgumentException)
        {
            throw new OutboxDispatchException("message_request_invalid", false, "The booking message request is invalid.");
        }

        var status = result.Accepted
            ? MessageDeliveryStatus.Accepted
            : result.IsTransientFailure
                ? MessageDeliveryStatus.TransientFailure
                : MessageDeliveryStatus.PermanentFailure;
        await RecordAttemptAsync(
            context,
            payload,
            result.ProviderCode ?? "unavailable",
            MaskEmail(contact.Value),
            status,
            result.ProviderMessageId,
            result.ErrorCode,
            result.TemplateVersion ?? 1,
            cancellationToken);
        if (!result.Accepted)
            throw new OutboxDispatchException(
                result.ErrorCode ?? "message_delivery_failed",
                result.IsTransientFailure,
                "The booking message provider did not accept the delivery.");
    }

    private async Task RecordAttemptAsync(
        OutboxMessageContext context,
        BookingConfirmedOutboxPayload payload,
        string providerCode,
        string recipientHint,
        MessageDeliveryStatus status,
        string? providerMessageId,
        string? errorCode,
        int templateVersion,
        CancellationToken cancellationToken)
    {
        await dbContext.MessageDeliveryAttempts.AddAsync(
            MessageDeliveryAttempt.Record(
                payload.TenantId,
                payload.BranchId,
                context.MessageId,
                context.OperationId,
                context.AttemptNumber,
                CommunicationChannel.Email,
                BookingConfirmedOutboxPayload.TemplateKey,
                templateVersion,
                providerCode,
                recipientHint,
                status,
                providerMessageId,
                errorCode,
                clock.UtcNow),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string MaskEmail(string address)
    {
        var separator = address.LastIndexOf('@');
        return separator > 0 && separator < address.Length - 1
            ? $"{address[0]}***{address[separator..]}"
            : "***";
    }
}
