namespace OperationsHub.Application.Reporting;

public sealed record DepartmentPerformanceDto(
    Guid? DepartmentId,
    string DepartmentName,
    int TotalRequests,
    int OpenRequests,
    int CompletedRequests,
    decimal? AverageResolutionHours,
    int SlaEligibleRequests,
    int SlaMetRequests)
{
    public decimal? SlaCompliancePercentage => SlaEligibleRequests == 0
        ? null
        : decimal.Round((decimal)SlaMetRequests * 100 / SlaEligibleRequests, 1, MidpointRounding.AwayFromZero);
}
