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
            .RequireRateLimiting(SignInRateLimitPolicy);
        endpoints.MapPost("/sign-out", SignOutAsync)
            .WithMetadata(new RequireAntiforgeryTokenAttribute(true))
            .RequireAuthorization();
        endpoints.MapGet("/api/antiforgery", IssueAntiforgeryToken)
            .RequireAuthorization();

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

    private static async Task<IResult> SignOutAsync(
        HttpContext context,
        SignInManager<ApplicationUser> signInManager)
    {
        var isImpersonating = context.User.HasClaim(
            claim => claim.Type == ImpersonationClaimTypes.OriginalAdministratorId);
        await signInManager.SignOutAsync();
        return Results.Redirect(isImpersonating ? "/sign-in" : "/");
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

    private sealed record AntiforgeryTokenResponse(string RequestToken, string HeaderName);
}
