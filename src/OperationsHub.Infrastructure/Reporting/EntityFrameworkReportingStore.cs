using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using OperationsHub.Application.Reporting;
using OperationsHub.Infrastructure.Persistence;

namespace OperationsHub.Infrastructure.Reporting;

public sealed class EntityFrameworkReportingStore : IReportingStore
{
    private readonly OperationsHubDbContext database;

    public EntityFrameworkReportingStore(OperationsHubDbContext database) => this.database = database;

    public async Task<IReadOnlyList<DepartmentPerformanceDto>> GetDepartmentPerformanceAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT department_id, department_name, total_requests, open_requests, completed_requests,
                   average_resolution_hours, sla_eligible_requests, sla_met_requests
            FROM vw_department_performance
            ORDER BY total_requests DESC, department_name
            """;

        var connection = database.Database.GetDbConnection();
        await database.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var reports = new List<DepartmentPerformanceDto>();
            while (await reader.ReadAsync(cancellationToken))
            {
                reports.Add(new DepartmentPerformanceDto(
                    reader.IsDBNull(0) ? null : reader.GetFieldValue<Guid>(0),
                    reader.GetString(1),
                    reader.GetInt32(2),
                    reader.GetInt32(3),
                    reader.GetInt32(4),
                    reader.IsDBNull(5) ? null : reader.GetDecimal(5),
                    reader.GetInt32(6),
                    reader.GetInt32(7)));
            }

            return reports;
        }
        finally
        {
            await database.Database.CloseConnectionAsync();
        }
    }
}
