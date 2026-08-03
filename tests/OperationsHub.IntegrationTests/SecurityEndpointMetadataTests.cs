using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OperationsHub.Application.Impersonation;
using OperationsHub.Application.ReferenceData;
using OperationsHub.Application.Requests;
using OperationsHub.Infrastructure.Identity;
using OperationsHub.Web.Api;
using OperationsHub.Web.Authentication;

namespace OperationsHub.IntegrationTests;

public sealed class SecurityEndpointMetadataTests
{
    [Fact]
    public async Task ReferenceDataEndpointsRequireAdministratorAndMutationsRequireAntiforgery()
    {
        await using var app = CreateApplication();
        app.MapReferenceDataEndpoints();

        var endpoints = GetRouteEndpoints(app)
            .Where(endpoint => endpoint.RoutePattern.RawText!.StartsWith("/api/reference-data", StringComparison.Ordinal))
            .ToList();

        Assert.Equal(10, endpoints.Count);
        Assert.All(
            endpoints,
            endpoint => Assert.Contains(
                endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>(),
                metadata => metadata.Policy == AuthorizationPolicies.Administrator));

        var mutationEndpoints = endpoints
            .Where(endpoint => !endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods.Contains(HttpMethods.Get, StringComparer.Ordinal))
            .ToList();

        Assert.Equal(8, mutationEndpoints.Count);
        Assert.All(
            mutationEndpoints,
            endpoint => Assert.True(endpoint.Metadata.GetMetadata<IAntiforgeryMetadata>()?.RequiresValidation));
    }

    [Fact]
    public async Task AuthenticationMutationsRequireAntiforgeryAndSignInIsRateLimited()
    {
        await using var app = CreateApplication();
        app.MapAuthenticationEndpoints();

        var endpoints = GetRouteEndpoints(app);
        var signIn = Assert.Single(endpoints, endpoint => endpoint.RoutePattern.RawText == "/sign-in");
        var signOut = Assert.Single(endpoints, endpoint => endpoint.RoutePattern.RawText == "/sign-out");
        var antiforgery = Assert.Single(endpoints, endpoint => endpoint.RoutePattern.RawText == "/api/antiforgery");

        Assert.True(signIn.Metadata.GetMetadata<IAntiforgeryMetadata>()?.RequiresValidation);
        Assert.Equal(
            AuthenticationEndpoints.SignInRateLimitPolicy,
            signIn.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName);
        Assert.True(signOut.Metadata.GetMetadata<IAntiforgeryMetadata>()?.RequiresValidation);
        Assert.NotEmpty(signOut.Metadata.GetOrderedMetadata<IAuthorizeData>());
        Assert.NotEmpty(antiforgery.Metadata.GetOrderedMetadata<IAuthorizeData>());
    }

    [Fact]
    public async Task ImpersonationMutationsRequireAntiforgeryAndExpectedAuthorization()
    {
        await using var app = CreateApplication();
        app.MapAdministratorImpersonationEndpoints();

        var endpoints = GetRouteEndpoints(app);
        var start = Assert.Single(
            endpoints,
            endpoint => endpoint.RoutePattern.RawText == "/administrator-impersonation/start");
        var end = Assert.Single(
            endpoints,
            endpoint => endpoint.RoutePattern.RawText == "/administrator-impersonation/end");

        Assert.True(start.Metadata.GetMetadata<IAntiforgeryMetadata>()?.RequiresValidation);
        Assert.Contains(
            start.Metadata.GetOrderedMetadata<IAuthorizeData>(),
            metadata => metadata.Policy == AuthorizationPolicies.AdministratorImpersonation);
        Assert.True(end.Metadata.GetMetadata<IAntiforgeryMetadata>()?.RequiresValidation);
        Assert.NotEmpty(end.Metadata.GetOrderedMetadata<IAuthorizeData>());
    }

    [Fact]
    public async Task ServiceRequestEndpointsRequireAuthenticationAndMutationsRequireAntiforgery()
    {
        await using var app = CreateApplication();
        app.MapServiceRequestEndpoints();

        var endpoints = GetRouteEndpoints(app)
            .Where(endpoint => endpoint.RoutePattern.RawText!.StartsWith("/api/requests", StringComparison.Ordinal))
            .ToList();

        Assert.Equal(8, endpoints.Count);
        Assert.All(endpoints, endpoint => Assert.NotEmpty(endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>()));
        Assert.All(
            endpoints.Where(endpoint => !endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods.Contains(HttpMethods.Get, StringComparer.Ordinal)),
            endpoint => Assert.True(endpoint.Metadata.GetMetadata<IAntiforgeryMetadata>()?.RequiresValidation));
    }

    private static WebApplication CreateApplication()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddAntiforgery();
        builder.Services.AddScoped<IReferenceDataAdministrationService>(_ => null!);
        builder.Services.AddScoped<IServiceRequestWorkflowService>(_ => null!);
        builder.Services.AddScoped<IAdministratorImpersonationService>(_ => null!);
        builder.Services.AddScoped<UserManager<ApplicationUser>>(_ => null!);
        builder.Services.AddScoped<SignInManager<ApplicationUser>>(_ => null!);
        builder.Services.AddSingleton(TimeProvider.System);
        return builder.Build();
    }

    private static List<RouteEndpoint> GetRouteEndpoints(WebApplication app) =>
        ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(dataSource => dataSource.Endpoints)
            .OfType<RouteEndpoint>()
            .ToList();
}
