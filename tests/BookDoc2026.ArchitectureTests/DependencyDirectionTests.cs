using System.Reflection;
using BookDoc2026.Application;
using BookDoc2026.Blazor.UI;
using BookDoc2026.Client;
using BookDoc2026.Contracts.Common;
using BookDoc2026.Domain.Foundation;
using BookDoc2026.DocumentService;
using BookDoc2026.ErrorHandling;
using BookDoc2026.Infrastructure;
using BookDoc2026.Messaging;
using BookDoc2026.Templates;
using BookDoc2026.Worker;

namespace BookDoc2026.ArchitectureTests;

public sealed class DependencyDirectionTests
{
    [Fact]
    public void Domain_DoesNotReferenceOuterLayers()
    {
        var references = References(typeof(Tenant).Assembly);

        Assert.DoesNotContain("BookDoc2026.Application", references);
        Assert.DoesNotContain("BookDoc2026.Contracts", references);
        Assert.DoesNotContain("BookDoc2026.Infrastructure", references);
        Assert.DoesNotContain("BookDoc2026.Api", references);
        Assert.DoesNotContain(references, name => name.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal));
    }

    [Fact]
    public void Contracts_DoNotReferenceBackendImplementation()
    {
        var references = References(typeof(ApiEnvelope<>).Assembly);

        Assert.DoesNotContain("BookDoc2026.Domain", references);
        Assert.DoesNotContain("BookDoc2026.Application", references);
        Assert.DoesNotContain("BookDoc2026.Infrastructure", references);
        Assert.DoesNotContain("BookDoc2026.Api", references);
    }

    [Fact]
    public void Application_DoesNotReferenceInfrastructureOrApi()
    {
        var references = References(typeof(ApplicationRegistration).Assembly);

        Assert.Contains("BookDoc2026.Domain", references);
        Assert.Contains("BookDoc2026.Contracts", references);
        Assert.DoesNotContain("BookDoc2026.Infrastructure", references);
        Assert.DoesNotContain("BookDoc2026.Api", references);
    }

    [Fact]
    public void Worker_IsALibraryConsumedByApiWithoutReferencingIt()
    {
        var workerAssembly = typeof(OutboxWorker).Assembly;
        var references = References(workerAssembly);
        var apiReferences = References(typeof(Program).Assembly);

        Assert.Contains("BookDoc2026.Application", references);
        Assert.Contains("BookDoc2026.Infrastructure", references);
        Assert.DoesNotContain("BookDoc2026.Api", references);
        Assert.Contains("BookDoc2026.Worker", apiReferences);
        Assert.Null(workerAssembly.EntryPoint);
    }

    [Fact]
    public void Infrastructure_ImplementsApplicationPorts()
    {
        var references = References(typeof(InfrastructureRegistration).Assembly);

        Assert.Contains("BookDoc2026.Application", references);
        Assert.Contains("BookDoc2026.Domain", references);
        Assert.Contains("BookDoc2026.Messaging", references);
        Assert.Contains("BookDoc2026.Templates", references);
        Assert.DoesNotContain("BookDoc2026.Api", references);
    }

    [Fact]
    public void TypedClient_DoesNotReferenceBackendLayers()
    {
        var references = References(typeof(BookDocApiClient).Assembly);

        Assert.Contains("BookDoc2026.Contracts", references);
        Assert.DoesNotContain("BookDoc2026.Domain", references);
        Assert.DoesNotContain("BookDoc2026.Application", references);
        Assert.DoesNotContain("BookDoc2026.Infrastructure", references);
        Assert.DoesNotContain("BookDoc2026.Api", references);
    }

    [Fact]
    public void SharedBlazorUi_DoesNotReferenceBackendLayers()
    {
        var references = References(typeof(BookDocUiRegistration).Assembly);

        Assert.DoesNotContain("BookDoc2026.Domain", references);
        Assert.DoesNotContain("BookDoc2026.Application", references);
        Assert.DoesNotContain("BookDoc2026.Infrastructure", references);
        Assert.DoesNotContain("BookDoc2026.Api", references);
    }

    [Fact]
    public void ServiceLibraries_AreApiComposedLibrariesWithExplicitDependencies()
    {
        var templateAssembly = typeof(StrictTemplateRenderer).Assembly;
        var messagingAssembly = typeof(MessageDispatcher).Assembly;
        var documentAssembly = typeof(DocumentGenerationService).Assembly;
        var templateReferences = References(templateAssembly);
        var messagingReferences = References(messagingAssembly);
        var documentReferences = References(documentAssembly);
        var apiReferences = References(typeof(Program).Assembly);

        Assert.DoesNotContain("BookDoc2026.Api", templateReferences);
        Assert.DoesNotContain("BookDoc2026.Application", templateReferences);
        Assert.Contains("BookDoc2026.Templates", messagingReferences);
        Assert.DoesNotContain("BookDoc2026.Api", messagingReferences);
        Assert.DoesNotContain("BookDoc2026.Infrastructure", messagingReferences);
        Assert.DoesNotContain("BookDoc2026.Api", documentReferences);
        Assert.DoesNotContain("BookDoc2026.Infrastructure", documentReferences);
        Assert.Contains("BookDoc2026.Messaging", apiReferences);
        Assert.Contains("BookDoc2026.DocumentService", apiReferences);
        Assert.Null(templateAssembly.EntryPoint);
        Assert.Null(messagingAssembly.EntryPoint);
        Assert.Null(documentAssembly.EntryPoint);
    }

    [Fact]
    public void ErrorHandling_IsAPlatformNeutralLibrarySharedByApiClientAndWorker()
    {
        var errorAssembly = typeof(ErrorDescriptor).Assembly;
        var references = References(errorAssembly);
        var apiReferences = References(typeof(Program).Assembly);
        var clientReferences = References(typeof(BookDocApiClient).Assembly);
        var workerReferences = References(typeof(OutboxWorker).Assembly);

        Assert.DoesNotContain("BookDoc2026.Api", references);
        Assert.DoesNotContain("BookDoc2026.Application", references);
        Assert.DoesNotContain("BookDoc2026.Domain", references);
        Assert.DoesNotContain("BookDoc2026.Infrastructure", references);
        Assert.Contains("BookDoc2026.ErrorHandling", apiReferences);
        Assert.Contains("BookDoc2026.ErrorHandling", clientReferences);
        Assert.Contains("BookDoc2026.ErrorHandling", workerReferences);
        Assert.Null(errorAssembly.EntryPoint);
    }

    private static HashSet<string> References(Assembly assembly) => assembly
        .GetReferencedAssemblies()
        .Select(reference => reference.Name!)
        .ToHashSet(StringComparer.Ordinal);
}
