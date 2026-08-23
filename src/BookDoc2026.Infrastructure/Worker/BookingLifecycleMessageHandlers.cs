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

internal sealed record BookingLifecycleMessageDefinition(string Purpose, string TemplateKey);

internal sealed class BookingLifecycleMessageDispatcher(
    BookDocDbContext dbContext,
    IMessageDispatcher dispatcher,
    IClock clock,
    ILogger<BookingLifecycleMessageDispatcher> logger)
{
    public async Task HandleAsync(
        OutboxMessageContext context,
        BookingLifecycleMessageDefinition definition,
        CancellationToken cancellationToken)
    {
        BookingLifecycleOutboxPayload payload;
        try
        {
            payload = JsonSerializer.Deserialize<BookingLifecycleOutboxPayload>(context.PayloadJson)
                ?? throw new JsonException();
        }
        catch (JsonException)
        {
            throw new OutboxDispatchException("invalid_booking_lifecycle_payload", false, "The booking lifecycle payload is invalid.");
        }

        if (context.TenantId != payload.TenantId || context.BranchId != payload.BranchId)
            throw new OutboxDispatchException("booking_lifecycle_scope_mismatch", false, "The booking lifecycle scope is invalid.");
        if (await dbContext.MessageDeliveryAttempts.IgnoreQueryFilters().AnyAsync(attempt =>
                attempt.TenantId == payload.TenantId && attempt.OperationId == context.OperationId
                && attempt.Channel == CommunicationChannel.Email
                && (attempt.Status == MessageDeliveryStatus.Accepted || attempt.Status == MessageDeliveryStatus.Suppressed),
                cancellationToken))
            return;

        var booking = await dbContext.Bookings.IgnoreQueryFilters().AsNoTracking().SingleOrDefaultAsync(candidate =>
                candidate.Id == payload.BookingId && candidate.TenantId == payload.TenantId,
                cancellationToken)
            ?? throw new OutboxDispatchException("booking_not_found", false, "The booking lifecycle source was not found.");
        if (booking.BranchId != payload.BranchId || booking.PatientId != payload.PatientId)
            throw new OutboxDispatchException("booking_lifecycle_source_mismatch", false, "The booking lifecycle source is invalid.");

        var latestPreference = await dbContext.CommunicationPreferenceEvents.IgnoreQueryFilters().AsNoTracking()
            .Where(preference => preference.TenantId == payload.TenantId
                && preference.BranchId == payload.BranchId
                && preference.StakeholderId == payload.StakeholderId
                && preference.PurposeCode == definition.Purpose
                && preference.MessageClass == CommunicationMessageClass.Transactional
                && preference.Channel == CommunicationChannel.Email)
            .OrderByDescending(preference => preference.CreatedUtc)
            .ThenByDescending(preference => preference.Id)
            .FirstOrDefaultAsync(cancellationToken);
        var preferenceResult = CommunicationPreferenceEvaluator.Evaluate(latestPreference, clock.UtcNow);
        if (!preferenceResult.CanSend)
        {
            await RecordAttemptAsync(context, payload, definition.TemplateKey, "policy", "***",
                MessageDeliveryStatus.Suppressed, null, preferenceResult.Code, 1, cancellationToken);
            return;
        }

        var contact = await dbContext.StakeholderContactPoints.IgnoreQueryFilters().AsNoTracking()
            .Where(candidate => candidate.TenantId == payload.TenantId
                && candidate.StakeholderId == payload.StakeholderId && candidate.Type == ContactPointType.Email)
            .OrderByDescending(candidate => candidate.IsPrimary).ThenBy(candidate => candidate.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (contact is null)
        {
            await RecordAttemptAsync(context, payload, definition.TemplateKey, "unavailable", "***",
                MessageDeliveryStatus.PermanentFailure, null, "recipient_email_missing", 1, cancellationToken);
            throw new OutboxDispatchException("recipient_email_missing", false, "The booking recipient is unavailable.");
        }

        var person = await dbContext.StakeholderPersons.IgnoreQueryFilters().AsNoTracking().SingleOrDefaultAsync(candidate =>
                candidate.TenantId == payload.TenantId && candidate.StakeholderId == payload.StakeholderId,
                cancellationToken)
            ?? throw new OutboxDispatchException("booking_recipient_not_found", false, "The booking recipient was not found.");
        var branch = await dbContext.Branches.IgnoreQueryFilters().AsNoTracking().SingleOrDefaultAsync(candidate =>
                candidate.Id == payload.BranchId && candidate.TenantId == payload.TenantId,
                cancellationToken)
            ?? throw new OutboxDispatchException("booking_branch_not_found", false, "The booking branch was not found.");
        var service = await dbContext.ClinicalServices.IgnoreQueryFilters().AsNoTracking().SingleOrDefaultAsync(candidate =>
                candidate.Id == booking.ServiceId && candidate.TenantId == payload.TenantId,
                cancellationToken)
            ?? throw new OutboxDispatchException("booking_service_not_found", false, "The booking service was not found.");

        var values = new Dictionary<string, string?>
        {
            ["PatientName"] = person.DisplayName,
            ["BookingNumber"] = booking.BookingNumber,
            ["ServiceName"] = service.Name,
            ["StartTime"] = FormatLocal(booking.StartUtc, branch.TimeZoneId),
            ["ClinicName"] = branch.Name,
            ["Reason"] = booking.CancellationReason
        };
        if (payload.PreviousBookingId.HasValue)
        {
            var previous = await dbContext.Bookings.IgnoreQueryFilters().AsNoTracking().SingleOrDefaultAsync(candidate =>
                    candidate.Id == payload.PreviousBookingId.Value && candidate.TenantId == payload.TenantId,
                    cancellationToken)
                ?? throw new OutboxDispatchException("previous_booking_not_found", false, "The previous booking was not found.");
            values["PreviousBookingNumber"] = previous.BookingNumber;
            values["PreviousStartTime"] = FormatLocal(previous.StartUtc, branch.TimeZoneId);
            values["Reason"] = previous.CancellationReason;
        }

        MessageDeliveryResult result;
        try
        {
            result = await dispatcher.DispatchAsync(
                new MessageDispatchRequest(
                    new TemplateScope(payload.TenantId, payload.OrganizationId, payload.BranchId),
                    MessageChannel.Email,
                    definition.TemplateKey,
                    "en-IN",
                    new MessageRecipient(contact.Value, person.DisplayName),
                    values,
                    context.OperationId),
                cancellationToken);
        }
        catch (OperationCanceledException) { throw; }
        catch (MessageConfigurationException exception)
        {
            logger.LogWarning("Booking lifecycle message configuration failed. ErrorCode={ErrorCode} CorrelationId={CorrelationId}",
                exception.ErrorCode, context.CorrelationId);
            throw new OutboxDispatchException(exception.ErrorCode, false, "The booking lifecycle message configuration is invalid.");
        }
        catch (TemplateRenderException)
        {
            throw new OutboxDispatchException("message_template_render_failed", false, "The booking lifecycle template could not be rendered.");
        }
        catch (ArgumentException)
        {
            throw new OutboxDispatchException("message_request_invalid", false, "The booking lifecycle message request is invalid.");
        }

        var status = result.Accepted ? MessageDeliveryStatus.Accepted
            : result.IsTransientFailure ? MessageDeliveryStatus.TransientFailure : MessageDeliveryStatus.PermanentFailure;
        await RecordAttemptAsync(context, payload, definition.TemplateKey, result.ProviderCode ?? "unavailable",
            MaskEmail(contact.Value), status, result.ProviderMessageId, result.ErrorCode,
            result.TemplateVersion ?? 1, cancellationToken);
        if (!result.Accepted)
            throw new OutboxDispatchException(result.ErrorCode ?? "message_delivery_failed", result.IsTransientFailure,
                "The booking lifecycle provider did not accept the delivery.");
    }

    private static string FormatLocal(DateTimeOffset value, string timeZoneId)
    {
        try
        {
            return TimeZoneInfo.ConvertTime(value, TimeZoneInfo.FindSystemTimeZoneById(timeZoneId))
                .ToString("dd MMM yyyy, hh:mm tt");
        }
        catch (Exception exception) when (exception is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            throw new OutboxDispatchException("branch_timezone_invalid", false, "The booking branch time zone is invalid.");
        }
    }

    private async Task RecordAttemptAsync(
        OutboxMessageContext context,
        BookingLifecycleOutboxPayload payload,
        string templateKey,
        string providerCode,
        string recipientHint,
        MessageDeliveryStatus status,
        string? providerMessageId,
        string? errorCode,
        int templateVersion,
        CancellationToken cancellationToken)
    {
        await dbContext.MessageDeliveryAttempts.AddAsync(MessageDeliveryAttempt.Record(
            payload.TenantId, payload.BranchId, context.MessageId, context.OperationId, context.AttemptNumber,
            CommunicationChannel.Email, templateKey, templateVersion, providerCode, recipientHint, status,
            providerMessageId, errorCode, clock.UtcNow), cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string MaskEmail(string address)
    {
        var separator = address.LastIndexOf('@');
        return separator > 0 && separator < address.Length - 1 ? $"{address[0]}***{address[separator..]}" : "***";
    }
}

internal sealed class BookingCancelledMessageHandler(BookingLifecycleMessageDispatcher dispatcher) : IOutboxMessageHandler
{
    public string MessageType => BookingLifecycleOutboxPayload.CancelledMessageType;
    public Task HandleAsync(OutboxMessageContext context, CancellationToken cancellationToken) =>
        dispatcher.HandleAsync(context, new(BookingLifecycleOutboxPayload.PreferencePurpose,
            BookingLifecycleOutboxPayload.CancelledTemplateKey), cancellationToken);
}

internal sealed class BookingRescheduledMessageHandler(BookingLifecycleMessageDispatcher dispatcher) : IOutboxMessageHandler
{
    public string MessageType => BookingLifecycleOutboxPayload.RescheduledMessageType;
    public Task HandleAsync(OutboxMessageContext context, CancellationToken cancellationToken) =>
        dispatcher.HandleAsync(context, new(BookingLifecycleOutboxPayload.PreferencePurpose,
            BookingLifecycleOutboxPayload.RescheduledTemplateKey), cancellationToken);
}

internal sealed class WaitlistPromotedMessageHandler(BookingLifecycleMessageDispatcher dispatcher) : IOutboxMessageHandler
{
    public string MessageType => BookingLifecycleOutboxPayload.WaitlistPromotedMessageType;
    public Task HandleAsync(OutboxMessageContext context, CancellationToken cancellationToken) =>
        dispatcher.HandleAsync(context, new(BookingLifecycleOutboxPayload.PreferencePurpose,
            BookingLifecycleOutboxPayload.WaitlistPromotedTemplateKey), cancellationToken);
}
