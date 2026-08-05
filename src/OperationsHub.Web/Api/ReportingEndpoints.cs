using System.Globalization;
using System.Security.Claims;
using System.Text;
using OperationsHub.Application.Reporting;
using OperationsHub.Application.Requests;
using OperationsHub.Infrastructure.Identity;

namespace OperationsHub.Web.Api;

public static class ReportingEndpoints
{
    public static IEndpointRouteBuilder MapReportingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/reports").RequireAuthorization();
        group.MapGet("/department-performance", GetDepartmentPerformanceAsync);
        group.MapGet("/department-performance.csv", DownloadDepartmentPerformanceCsvAsync);
        return endpoints;
    }

    private static async Task<IResult> GetDepartmentPerformanceAsync(
        HttpContext context,
        IRequestReportingService service,
        CancellationToken cancellationToken) =>
        ToResult(await service.GetDepartmentPerformanceAsync(GetActor(context.User), cancellationToken));

    private static async Task<IResult> DownloadDepartmentPerformanceCsvAsync(
        HttpContext context,
        IRequestReportingService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetDepartmentPerformanceAsync(GetActor(context.User), cancellationToken);
        if (!result.Succeeded)
        {
            return ToResult(result);
        }

        var csv = new StringBuilder();
        csv.AppendLine("Department,Total requests,Open requests,Completed requests,Average resolution hours,SLA eligible requests,SLA met requests,SLA compliance percentage");
        foreach (var row in result.Value!)
        {
            csv.AppendLine(string.Join(",",
                Escape(row.DepartmentName),
                row.TotalRequests,
                row.OpenRequests,
                row.CompletedRequests,
                FormatDecimal(row.AverageResolutionHours),
                row.SlaEligibleRequests,
                row.SlaMetRequests,
                FormatDecimal(row.SlaCompliancePercentage)));
        }

        return Results.File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "department-performance.csv");
    }

    private static RequestActor GetActor(ClaimsPrincipal user)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("Authenticated user ID is missing.");
        var role = user.IsInRole(RoleNames.Administrator)
            ? RequestActorRole.Administrator
            : user.IsInRole(RoleNames.Manager)
                ? RequestActorRole.Manager
                : user.IsInRole(RoleNames.Technician)
                    ? RequestActorRole.Technician
                    : RequestActorRole.Requester;
        return new RequestActor(userId, role);
    }

    private static IResult ToResult<T>(RequestOperationResult<T> result) => result.Status switch
    {
        RequestOperationStatus.Success => Results.Ok(result.Value),
        RequestOperationStatus.Forbidden => Results.Forbid(),
        _ => Results.Problem(),
    };

    private static string Escape(string value) => $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";

    private static string FormatDecimal(decimal? value) => value?.ToString("0.0", CultureInfo.InvariantCulture) ?? string.Empty;
}
