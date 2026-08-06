using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using OperationsHub.Infrastructure.Identity;

namespace OperationsHub.Web.Authentication;

public static class AuthenticationEndpoints
{
    public const string SignInRateLimitPolicy = "sign-in";

    public static IEndpointRouteBuilder MapAuthenticationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/sign-in", SignInAsync)
            .WithMetadata(new RequireAntiforgeryTokenAttribute(true))
            .RequireRateLimiting(SignInRateLimitPolicy)
            .ExcludeFromDescription();
        endpoints.MapPost("/sign-out", SignOutAsync)
            .WithMetadata(new RequireAntiforgeryTokenAttribute(true))
            .RequireAuthorization()
            .ExcludeFromDescription();
        endpoints.MapGet("/api/antiforgery", IssueAntiforgeryToken)
            .RequireAuthorization()
            .WithTags("Authentication")
            .WithName("GetAntiforgeryToken")
            .WithSummary("Issue antiforgery state")
            .WithDescription("Stores an antiforgery cookie and returns the request token plus required header name for subsequent cookie-authenticated JSON mutations.")
            .Produces<AntiforgeryTokenResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return endpoints;
    }

    private static async Task<IResult> SignInAsync(
        [FromForm] SignInInput input,
        SignInManager<ApplicationUser> signInManager)
    {
        if (string.IsNullOrWhiteSpace(input.Email)
            || string.IsNullOrWhiteSpace(input.Password)
            || input.Email.Length > 256
            || input.Password.Length > 256)
        {
            return Results.Redirect("/sign-in?error=invalid");
        }

        var result = await signInManager.PasswordSignInAsync(
            input.Email.Trim(),
            input.Password,
            isPersistent: false,
            lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            return Results.Redirect("/sign-in?error=invalid");
        }

        return Results.Redirect(GetSafeReturnUrl(input.ReturnUrl));
    }

    private static async Task<IResult> SignOutAsync(SignInManager<ApplicationUser> signInManager)
    {
        await signInManager.SignOutAsync();
        return Results.Redirect("/");
    }

    private static IResult IssueAntiforgeryToken(HttpContext context, IAntiforgery antiforgery)
    {
        var tokens = antiforgery.GetAndStoreTokens(context);
        context.Response.Headers["Cache-Control"] = "no-store";
        return Results.Ok(new AntiforgeryTokenResponse(tokens.RequestToken!, tokens.HeaderName!));
    }

    private static string GetSafeReturnUrl(string? returnUrl) =>
        !string.IsNullOrWhiteSpace(returnUrl)
        && returnUrl.StartsWith('/')
        && !returnUrl.StartsWith("//", StringComparison.Ordinal)
        && !returnUrl.StartsWith("/\\", StringComparison.Ordinal)
            ? returnUrl
            : "/";

    private sealed class SignInInput
    {
        public string? Email { get; init; }

        public string? Password { get; init; }

        public string? ReturnUrl { get; init; }
    }

    public sealed record AntiforgeryTokenResponse(string RequestToken, string HeaderName);
}
