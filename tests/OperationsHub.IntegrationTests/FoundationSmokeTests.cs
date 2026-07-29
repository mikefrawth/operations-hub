using System.Reflection;
using OperationsHub.Application;
using OperationsHub.Domain;
using OperationsHub.Infrastructure;
using OperationsHub.Web.Components;

namespace OperationsHub.IntegrationTests;

public sealed class FoundationSmokeTests
{
    [Fact]
    public void SolutionAssembliesLoadWithExpectedNames()
    {
        var assemblyNames = new[]
        {
            typeof(DomainAssembly).Assembly.GetName().Name!,
            typeof(ApplicationAssembly).Assembly.GetName().Name!,
            typeof(InfrastructureAssembly).Assembly.GetName().Name!,
            typeof(App).Assembly.GetName().Name!,
        };

        Assert.Equal(
            [
                "OperationsHub.Domain",
                "OperationsHub.Application",
                "OperationsHub.Infrastructure",
                "OperationsHub.Web",
            ],
            assemblyNames);
    }

    [Fact]
    public void InfrastructureDoesNotReferenceWeb()
    {
        var references = GetOperationsHubReferences(typeof(InfrastructureAssembly).Assembly);

        Assert.DoesNotContain("OperationsHub.Web", references);
    }

    private static string[] GetOperationsHubReferences(Assembly assembly) =>
        assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name is not null && name.StartsWith("OperationsHub.", StringComparison.Ordinal))
            .Cast<string>()
            .ToArray();
}
