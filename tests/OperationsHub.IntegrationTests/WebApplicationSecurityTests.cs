using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OperationsHub.Application.Requests;
using OperationsHub.Infrastructure.Identity;

namespace OperationsHub.IntegrationTests;

public sealed class WebApplicationSecurityTests : IClassFixture<OperationsHubWebApplicationFactory>
{
    private readonly OperationsHubWebApplicationFactory factory;

    public WebApplicationSecurityTests(OperationsHubWebApplicationFactory factory) => this.factory = factory;

    [Fact]
    public async Task RequestApiReturnsUnauthorizedForAnAnonymousCaller()
    {
        using var client = CreateClient();

        using var response = await client.GetAsync("/api/requests/", CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ReportApiReturnsForbiddenForARequester()
    {
        using var client = CreateClient(RoleNames.Requester, "http-requester");

        using var response = await client.GetAsync("/api/reports/department-performance", CancellationToken.None);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RequestApiReturnsForbiddenWhenAuthenticatedUserHasNoOperationsRole()
    {
        using var client = CreateClient("Unrecognized", "http-unrecognized");

        using var response = await client.GetAsync("/api/requests/", CancellationToken.None);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ClassificationApiRejectsMissingAntiforgeryTokenAndRateLimitsRepeatedCalls()
    {
        using var client = await CreateRequesterCookieClientAsync();
        var command = new RequestClassificationCommand("Cannot sign in", "I am locked out.");

        var statuses = new List<HttpStatusCode>();
        for (var attempt = 1; attempt <= 11; attempt++)
        {
            using var response = await client.PostAsJsonAsync("/api/requests/classification", command, CancellationToken.None);
            statuses.Add(response.StatusCode);
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                break;
            }
        }

        Assert.Contains(HttpStatusCode.BadRequest, statuses);
        Assert.Equal(HttpStatusCode.TooManyRequests, statuses[^1]);
        Assert.All(statuses.Take(statuses.Count - 1), status => Assert.Equal(HttpStatusCode.BadRequest, status));
    }

    [Fact]
    public async Task ClassificationApiAcceptsAnAuthenticatedRequesterWithAValidAntiforgeryToken()
    {
        using var client = await CreateRequesterCookieClientAsync();
        using var tokenResponse = await client.GetAsync("/api/antiforgery", CancellationToken.None);
        tokenResponse.EnsureSuccessStatusCode();
        using var tokenDocument = JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync(CancellationToken.None));
        var requestToken = tokenDocument.RootElement.GetProperty("requestToken").GetString();
        var headerName = tokenDocument.RootElement.GetProperty("headerName").GetString();
        Assert.False(string.IsNullOrWhiteSpace(requestToken));
        Assert.False(string.IsNullOrWhiteSpace(headerName));
        client.DefaultRequestHeaders.Add(headerName!, requestToken);

        using var response = await client.PostAsJsonAsync(
            "/api/requests/classification",
            new RequestClassificationCommand("Cannot sign in", "I am locked out."),
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private HttpClient CreateClient(string? role = null, string? userId = null)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost"),
        });
        if (role is not null)
        {
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, role);
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, userId ?? Guid.NewGuid().ToString());
        }

        return client;
    }

    private async Task<HttpClient> CreateRequesterCookieClientAsync()
    {
        var client = CreateClient();
        using var signInPage = await client.GetAsync("/sign-in", CancellationToken.None);
        signInPage.EnsureSuccessStatusCode();
        var html = await signInPage.Content.ReadAsStringAsync(CancellationToken.None);
        var tokenMatch = Regex.Match(
            html,
            "name=\"__RequestVerificationToken\"[^>]*value=\"(?<token>[^\"]+)\"",
            RegexOptions.CultureInvariant);
        Assert.True(tokenMatch.Success, "The sign-in page did not contain an antiforgery token.");
        var token = WebUtility.HtmlDecode(tokenMatch.Groups["token"].Value);
        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["email"] = "requester@operationshub.local",
            ["password"] = "OperationsHub!2026",
            ["returnUrl"] = "/",
            ["__RequestVerificationToken"] = token,
        });
        using var signInResponse = await client.PostAsync("/sign-in", form, CancellationToken.None);
        Assert.Equal(HttpStatusCode.Redirect, signInResponse.StatusCode);
        Assert.Equal("/", signInResponse.Headers.Location?.OriginalString);
        return client;
    }
}

public sealed class OperationsHubWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string AuthenticationSelectorScheme = "OperationsHubIntegrationTestSelector";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var connectionString = Environment.GetEnvironmentVariable("OPERATIONS_HUB_TEST_CONNECTION")
            ?? "Server=127.0.0.1;Port=3307;Database=operationshub;User=operationshub;Password=operationshub_dev_only;SslMode=Disabled";

        builder.UseEnvironment(Environments.Development);
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:OperationsHub"] = connectionString,
            }));
        builder.ConfigureTestServices(services =>
        {
            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = AuthenticationSelectorScheme;
                    options.DefaultChallengeScheme = AuthenticationSelectorScheme;
                    options.DefaultForbidScheme = AuthenticationSelectorScheme;
                })
                .AddPolicyScheme(
                    AuthenticationSelectorScheme,
                    null,
                    options => options.ForwardDefaultSelector = context =>
                        context.Request.Headers.ContainsKey(TestAuthenticationHandler.RoleHeader)
                            ? TestAuthenticationHandler.SchemeName
                            : IdentityConstants.ApplicationScheme)
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                    TestAuthenticationHandler.SchemeName,
                    _ => { });
        });
    }
}

public sealed class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "OperationsHubIntegrationTest";
    public const string RoleHeader = "X-Test-Role";
    public const string UserIdHeader = "X-Test-User";

    public TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(RoleHeader, out var role) || string.IsNullOrWhiteSpace(role))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var userId = Request.Headers.TryGetValue(UserIdHeader, out var suppliedUserId)
            ? suppliedUserId.ToString()
            : Guid.NewGuid().ToString();
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, userId),
            new Claim(ClaimTypes.Role, role.ToString()),
        ], SchemeName);
        var principal = new ClaimsPrincipal(identity);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
    }
}
