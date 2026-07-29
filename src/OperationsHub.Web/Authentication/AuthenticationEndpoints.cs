using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using OperationsHub.Infrastructure.Identity;

namespace OperationsHub.Web.Authentication;

public static class AuthenticationEndpoints
{
    public static IEndpointRouteBuilder MapAuthenticationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/sign-in", SignInAsync);
        endpoints.MapPost("/sign-out", SignOutAsync)
            .WithMetadata(new RequireAntiforgeryTokenAttribute(true))
            .RequireAuthorization();

        return endpoints;
    }

    private static async Task<IResult> SignInAsync(
        [FromForm] SignInInput input,
        SignInManager<ApplicationUser> signInManager)
    {
        if (string.IsNullOrWhiteSpace(input.Email) || string.IsNullOrWhiteSpace(input.Password))
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

    private static string GetSafeReturnUrl(string? returnUrl) =>
        !string.IsNullOrWhiteSpace(returnUrl)
        && returnUrl.StartsWith('/')
        && !returnUrl.StartsWith("//", StringComparison.Ordinal)
        && !returnUrl.StartsWith("/\\", StringComparison.Ordinal)
            ? returnUrl
            : "/administration/reference-data";

    private sealed class SignInInput
    {
        public string? Email { get; init; }

        public string? Password { get; init; }

        public string? ReturnUrl { get; init; }
    }
}
