using BookDoc2026.DocumentService;
using BookDoc2026.Messaging;
using BookDoc2026.Templates;
using DocumentFormat.OpenXml.Packaging;

namespace BookDoc2026.UnitTests;

public sealed class ServiceLibraryTests
{
    [Fact]
    public void TemplateRenderer_RendersStrictVersionedHtmlAndEncodesValues()
    {
        var definition = new TemplateDefinition(
            "Booking.Confirmed.Patient",
            2,
            TemplateChannel.Email,
            "en-IN",
            TemplateContentKind.Html,
            "<p>Hello {{PatientName}}, booking {{BookingNumber}} is confirmed.</p>",
            "Booking {{BookingNumber}}");
        var values = new Dictionary<string, string?>
        {
            ["PatientName"] = "A & B",
            ["BookingNumber"] = "BK-100"
        };

        var rendered = new StrictTemplateRenderer().Render(new TemplateRenderRequest(definition, values));

        Assert.Equal(2, rendered.Version);
        Assert.Equal("Booking BK-100", rendered.Subject);
        Assert.Contains("A &amp; B", rendered.Body, StringComparison.Ordinal);
    }

    [Fact]
    public void TemplateRenderer_RejectsMissingRequiredValue()
    {
        var definition = new TemplateDefinition(
            "Booking.Reminder.Patient",
            1,
            TemplateChannel.Sms,
            "en-IN",
            TemplateContentKind.PlainText,
            "Booking {{BookingNumber}} is at {{StartTime}}.");

        var exception = Assert.Throws<TemplateRenderException>(() =>
            new StrictTemplateRenderer().Render(new TemplateRenderRequest(
                definition,
                new Dictionary<string, string?> { ["BookingNumber"] = "BK-101" })));

        Assert.Contains("StartTime", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MessagingDispatcher_ResolvesRendersAndSendsThroughMatchingProvider()
    {
        var definition = new TemplateDefinition(
            "Booking.Confirmed.Patient",
            3,
            TemplateChannel.WhatsApp,
            "en-IN",
            TemplateContentKind.PlainText,
            "Booking {{BookingNumber}} confirmed.");
        var catalog = new StubTemplateCatalog(definition);
        var provider = new CapturingMessageProvider(MessageChannel.WhatsApp);
        var dispatcher = new MessageDispatcher(catalog, new StrictTemplateRenderer(), [provider]);
        var request = new MessageDispatchRequest(
            new TemplateScope(10, 20, 30),
            MessageChannel.WhatsApp,
            definition.Key,
            definition.Culture,
            new MessageRecipient("+919999999999"),
            new Dictionary<string, string?> { ["BookingNumber"] = "BK-102" },
            Guid.NewGuid());

        var result = await dispatcher.DispatchAsync(request);

        Assert.True(result.Accepted);
        Assert.NotNull(provider.Message);
        Assert.Equal(3, provider.Message!.TemplateVersion);
        Assert.Equal("Booking BK-102 confirmed.", provider.Message.Body);
        Assert.Equal(request.IdempotencyKey, provider.Message.IdempotencyKey);
    }

    [Fact]
    public async Task DocumentService_GeneratesValidOpenXmlWordDocument()
    {
        var service = new DocumentGenerationService([new OpenXmlWordExporter()]);

        var generated = await service.GenerateAsync(
            DocumentOutputFormat.WordOpenXml,
            SampleDocument());

        Assert.Equal("patient-summary.docx", generated.FileName);
        Assert.Equal(64, generated.Sha256.Length);
        using var stream = new MemoryStream(generated.Content.ToArray());
        using var word = WordprocessingDocument.Open(stream, false);
        var bodyText = word.MainDocumentPart?.Document?.Body?.InnerText;
        Assert.NotNull(bodyText);
        Assert.Contains("Patient Summary", bodyText!, StringComparison.Ordinal);
        Assert.Contains("No known allergies", bodyText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DocumentService_GeneratesPdfWithStableMetadata()
    {
        var service = new DocumentGenerationService([new PdfDocumentExporter()]);

        var generated = await service.GenerateAsync(DocumentOutputFormat.Pdf, SampleDocument());

        Assert.Equal("patient-summary.pdf", generated.FileName);
        Assert.Equal("application/pdf", generated.ContentType);
        Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString(generated.Content.Span[..4]));
        Assert.Equal(64, generated.Sha256.Length);
    }

    private static DocumentContent SampleDocument() => new(
        "patient summary",
        "Patient Summary",
        [new DocumentSection("Clinical alerts", ["No known allergies"])],
        "Authorized patient summary",
        "BOOKDOC2026");

    private sealed class StubTemplateCatalog(TemplateDefinition definition) : ITemplateCatalog
    {
        public ValueTask<TemplateDefinition?> ResolvePublishedAsync(
            TemplateSelection selection,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<TemplateDefinition?>(definition);
    }

    private sealed class CapturingMessageProvider(MessageChannel channel) : IMessageProvider
    {
        public string ProviderCode => "capture-test";

        public MessageChannel Channel => channel;

        public MessageEnvelope? Message { get; private set; }

        public ValueTask<MessageDeliveryResult> SendAsync(
            MessageEnvelope message,
            CancellationToken cancellationToken = default)
        {
            Message = message;
            return ValueTask.FromResult(MessageDeliveryResult.Sent("provider-1"));
        }
    }
}
