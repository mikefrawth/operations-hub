using Microsoft.AspNetCore.Antiforgery;
using Microsoft.OpenApi;

namespace OperationsHub.Web.Api;

public static class AntiforgeryEndpointConventionExtensions
{
    public const string HeaderName = "RequestVerificationToken";

    public static RouteHandlerBuilder RequireAntiforgeryValidation(this RouteHandlerBuilder builder)
    {
        builder.WithMetadata(new RequireAntiforgeryTokenAttribute(true));
        builder.AddOpenApiOperationTransformer((operation, _, _) =>
        {
            operation.Parameters ??= [];
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = HeaderName,
                In = ParameterLocation.Header,
                Required = true,
                Description = "Request token returned by GET /api/antiforgery. Retain both the authentication and antiforgery cookies.",
                Schema = new OpenApiSchema { Type = JsonSchemaType.String },
            });
            return Task.CompletedTask;
        });

        // JSON minimal APIs have no form binder to reject a failed antiforgery feature, so validate explicitly.
        return builder.AddEndpointFilter(async (invocationContext, next) =>
        {
            var validationFeature = invocationContext.HttpContext.Features.Get<IAntiforgeryValidationFeature>();
            if (validationFeature is { IsValid: false })
            {
                return InvalidToken();
            }

            var antiforgery = invocationContext.HttpContext.RequestServices.GetRequiredService<IAntiforgery>();
            try
            {
                if (validationFeature is null)
                {
                    await antiforgery.ValidateRequestAsync(invocationContext.HttpContext);
                }
            }
            catch (AntiforgeryValidationException)
            {
                return InvalidToken();
            }

            return await next(invocationContext);
        });
    }

    private static IResult InvalidToken() => Results.Problem(
        statusCode: StatusCodes.Status400BadRequest,
        title: "The antiforgery token is invalid.");
}
