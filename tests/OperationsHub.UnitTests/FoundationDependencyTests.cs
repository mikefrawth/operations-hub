using System.Reflection;
using OperationsHub.Application;
using OperationsHub.Domain;

namespace OperationsHub.UnitTests;

public sealed class FoundationDependencyTests
{
    [Fact]
    public void DomainDoesNotReferenceAnotherOperationsHubProject()
    {
        var references = GetOperationsHubReferences(typeof(DomainAssembly).Assembly);

        Assert.Empty(references);
    }

    [Fact]
    public void ApplicationDoesNotReferenceAnOuterLayer()
    {
        var references = GetOperationsHubReferences(typeof(ApplicationAssembly).Assembly);

        Assert.DoesNotContain("OperationsHub.Infrastructure", references);
        Assert.DoesNotContain("OperationsHub.Web", references);
    }

    private static string[] GetOperationsHubReferences(Assembly assembly) =>
        assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name is not null && name.StartsWith("OperationsHub.", StringComparison.Ordinal))
            .Cast<string>()
            .ToArray();
}
