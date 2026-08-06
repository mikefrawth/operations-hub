using Microsoft.OpenApi;

namespace OperationsHub.Web.Api;

public static class OpenApiServiceCollectionExtensions
{
    private const string CookieSecurityScheme = "IdentityCookie";

    public static IServiceCollection AddOperationsHubOpenApi(this IServiceCollection services) =>
        services.AddOpenApi(options =>
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info.Title = "OperationsHub API";
                document.Info.Description = "Cookie-authenticated service-request, reporting, and reference-data operations.";

                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
                {
                    [CookieSecurityScheme] = new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.ApiKey,
                        In = ParameterLocation.Cookie,
                        Name = ".AspNetCore.Identity.Application",
                        Description = "ASP.NET Core Identity application cookie obtained through the antiforgery-protected /sign-in form.",
                    },
                };

                foreach (var operation in document.Paths.Values.SelectMany(path => path.Operations ?? []))
                {
                    operation.Value.Security ??= [];
                    operation.Value.Security.Add(new OpenApiSecurityRequirement
                    {
                        [new OpenApiSecuritySchemeReference(CookieSecurityScheme, document)] = [],
                    });
                }

                return Task.CompletedTask;
            }));
}
