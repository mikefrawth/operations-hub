using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using OperationsHub.Web.Api;

namespace OperationsHub.IntegrationTests;

public sealed class OpenApiDocumentTests : IClassFixture<OperationsHubWebApplicationFactory>
{
    private readonly OperationsHubWebApplicationFactory factory;

    public OpenApiDocumentTests(OperationsHubWebApplicationFactory factory) => this.factory = factory;

    [Fact]
    public async Task DevelopmentDocumentDescribesSecuredApiOperationsAndAntiforgery()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost"),
        });

        using var response = await client.GetAsync("/openapi/v1.json", CancellationToken.None);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(CancellationToken.None));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.StartsWith("3.", document.RootElement.GetProperty("openapi").GetString(), StringComparison.Ordinal);
        Assert.Equal("OperationsHub API", document.RootElement.GetProperty("info").GetProperty("title").GetString());

        var paths = document.RootElement.GetProperty("paths");
        Assert.Equal(18, paths.EnumerateObject().Count());
        Assert.Equal(22, paths.EnumerateObject().Sum(path => path.Value.EnumerateObject().Count()));
        Assert.All(paths.EnumerateObject(), path => Assert.StartsWith("/api/", path.Name, StringComparison.Ordinal));
        Assert.False(paths.TryGetProperty("/health", out _));
        Assert.False(paths.TryGetProperty("/sign-in", out _));

        var createOperation = GetPath(paths, "/api/requests").GetProperty("post");
        Assert.Equal("Create a service request", createOperation.GetProperty("summary").GetString());
        Assert.Contains(
            createOperation.GetProperty("parameters").EnumerateArray(),
            parameter =>
                parameter.GetProperty("name").GetString() == AntiforgeryEndpointConventionExtensions.HeaderName
                && parameter.GetProperty("in").GetString() == "header"
                && parameter.GetProperty("required").GetBoolean()
                && parameter.GetProperty("schema").GetProperty("type").GetString() == "string");
        var responses = createOperation.GetProperty("responses");
        Assert.True(responses.TryGetProperty("201", out _));
        Assert.True(responses.TryGetProperty("400", out _));
        Assert.True(responses.TryGetProperty("401", out _));
        Assert.True(responses.TryGetProperty("403", out _));
        Assert.Contains(
            createOperation.GetProperty("security").EnumerateArray(),
            requirement => requirement.TryGetProperty("IdentityCookie", out _));

        var cookieScheme = document.RootElement
            .GetProperty("components")
            .GetProperty("securitySchemes")
            .GetProperty("IdentityCookie");
        Assert.Equal("apiKey", cookieScheme.GetProperty("type").GetString());
        Assert.Equal("cookie", cookieScheme.GetProperty("in").GetString());
        Assert.Equal(".AspNetCore.Identity.Application", cookieScheme.GetProperty("name").GetString());
    }

    [Fact]
    public async Task ProductionDoesNotExposeOpenApiDocumentOrInteractiveUi()
    {
        await using var productionFactory = new ProductionOperationsHubWebApplicationFactory();
        using var client = productionFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost"),
        });

        using var documentResponse = await client.GetAsync("/openapi/v1.json", CancellationToken.None);
        using var uiResponse = await client.GetAsync("/swagger", CancellationToken.None);

        Assert.Equal(HttpStatusCode.NotFound, documentResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, uiResponse.StatusCode);
    }

    private static JsonElement GetPath(JsonElement paths, string path)
    {
        foreach (var candidate in paths.EnumerateObject())
        {
            if (string.Equals(candidate.Name.TrimEnd('/'), path, StringComparison.Ordinal))
            {
                return candidate.Value;
            }
        }

        throw new Xunit.Sdk.XunitException($"The OpenAPI document did not contain '{path}'.");
    }
}

public sealed class ProductionOperationsHubWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var connectionString = Environment.GetEnvironmentVariable("OPERATIONS_HUB_TEST_CONNECTION")
            ?? "Server=127.0.0.1;Port=3307;Database=operationshub;User=operationshub;Password=operationshub_dev_only;SslMode=Disabled";

        builder.UseEnvironment(Environments.Production);
        builder.UseSetting("ConnectionStrings:OperationsHub", connectionString);
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:OperationsHub"] = connectionString,
            }));
    }
}
