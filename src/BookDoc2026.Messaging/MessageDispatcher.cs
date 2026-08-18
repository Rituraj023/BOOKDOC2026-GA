using BookDoc2026.Templates;

namespace BookDoc2026.Messaging;

public sealed class MessageDispatcher(
    ITemplateCatalog templates,
    ITemplateRenderer renderer,
    IEnumerable<IMessageProvider> providers) : IMessageDispatcher
{
    private readonly IReadOnlyDictionary<MessageChannel, IMessageProvider> _providers = providers
        .GroupBy(provider => provider.Channel)
        .ToDictionary(
            group => group.Key,
            group => group.Single());

    public async ValueTask<MessageDeliveryResult> DispatchAsync(
        MessageDispatchRequest request,
        CancellationToken cancellationToken = default)
    {
        Validate(request);

        if (!_providers.TryGetValue(request.Channel, out var provider))
        {
            throw new MessageConfigurationException(
                "message_provider_not_configured",
                "No message provider is configured for the requested channel.");
        }

        var templateChannel = ToTemplateChannel(request.Channel);
        var selection = new TemplateSelection(
            request.Scope,
            request.TemplateKey,
            templateChannel,
            request.Culture,
            request.TemplateVersion);

        var definition = await templates.ResolvePublishedAsync(selection, cancellationToken)
            ?? throw new MessageConfigurationException(
                "message_template_not_found",
                "No published template is configured for the requested message.");

        if (definition.Channel != templateChannel)
        {
            throw new MessageConfigurationException(
                "message_template_channel_mismatch",
                "The published template does not match the requested message channel.");
        }

        var rendered = renderer.Render(new TemplateRenderRequest(definition, request.Values));
        var envelope = new MessageEnvelope(
            request.Scope,
            request.Channel,
            request.Recipient,
            rendered.Key,
            rendered.Version,
            rendered.Culture,
            rendered.Subject,
            rendered.Body,
            rendered.ContentKind == TemplateContentKind.Html ? "text/html" : "text/plain",
            request.IdempotencyKey);

        var result = await provider.SendAsync(envelope, cancellationToken);
        return result with
        {
            ProviderCode = provider.ProviderCode,
            TemplateVersion = rendered.Version
        };
    }

    private static TemplateChannel ToTemplateChannel(MessageChannel channel) => channel switch
    {
        MessageChannel.Email => TemplateChannel.Email,
        MessageChannel.Sms => TemplateChannel.Sms,
        MessageChannel.WhatsApp => TemplateChannel.WhatsApp,
        MessageChannel.Push => TemplateChannel.Push,
        _ => throw new ArgumentOutOfRangeException(nameof(channel), channel, "Unsupported message channel.")
    };

    private static void Validate(MessageDispatchRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Scope);
        ArgumentNullException.ThrowIfNull(request.Recipient);
        ArgumentNullException.ThrowIfNull(request.Values);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.TemplateKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Culture);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Recipient.Address);

        if (request.Scope.TenantId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Tenant scope must be positive.");
        }

        if (request.IdempotencyKey == Guid.Empty)
        {
            throw new ArgumentException("A non-empty idempotency key is required.", nameof(request));
        }
    }
}
