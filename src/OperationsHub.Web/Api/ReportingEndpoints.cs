using System.Globalization;
using System.Text;
using OperationsHub.Application.Reporting;
using OperationsHub.Application.Requests;
using OperationsHub.Web.Authentication;

namespace OperationsHub.Web.Api;

public static class ReportingEndpoints
{
    public static IEndpointRouteBuilder MapReportingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/reports")
            .RequireAuthorization(AuthorizationPolicies.ManagerOrAdministrator)
            .WithTags("Reporting")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
        group.MapGet("/department-performance", GetDepartmentPerformanceAsync)
            .WithName("GetDepartmentPerformance")
            .WithSummary("Get department performance")
            .WithDescription("Returns the all-time MySQL reporting-view projection for managers and administrators.")
            .Produces<IReadOnlyList<DepartmentPerformanceDto>>();
        group.MapGet("/department-performance.csv", DownloadDepartmentPerformanceCsvAsync)
            .WithName("DownloadDepartmentPerformanceCsv")
            .WithSummary("Download department performance as CSV")
            .WithDescription("Returns the same manager/administrator report as a formula-neutralized CSV file.")
            .Produces(StatusCodes.Status200OK, contentType: "text/csv");
        return endpoints;
    }

    private static async Task<IResult> GetDepartmentPerformanceAsync(
        HttpContext context,
        IRequestReportingService service,
        CancellationToken cancellationToken) =>
        ToResult(await service.GetDepartmentPerformanceAsync(RequestActorClaimsMapper.Create(context.User), cancellationToken));

    private static async Task<IResult> DownloadDepartmentPerformanceCsvAsync(
        HttpContext context,
        IRequestReportingService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetDepartmentPerformanceAsync(RequestActorClaimsMapper.Create(context.User), cancellationToken);
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

    private static IResult ToResult<T>(RequestOperationResult<T> result) => result.Status switch
    {
        RequestOperationStatus.Success => Results.Ok(result.Value),
        RequestOperationStatus.Forbidden => Results.Forbid(),
        _ => Results.Problem(),
    };

    private static string Escape(string value)
    {
        // Spreadsheet applications can execute leading formula characters in an otherwise valid CSV cell.
        var safeValue = value.Length > 0 && value[0] is '=' or '+' or '-' or '@' or '\t' or '\r' or '\n' ? $"'{value}" : value;
        return $"\"{safeValue.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }

    private static string FormatDecimal(decimal? value) => value?.ToString("0.0", CultureInfo.InvariantCulture) ?? string.Empty;
}
