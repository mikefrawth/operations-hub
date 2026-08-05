using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1861 // EF migration operations use inline key and column arrays.
#pragma warning disable IDE0161 // Keep the EF-generated block-scoped migration namespace.

namespace OperationsHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDepartmentPerformanceReporting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_service_requests_department_id_status_created_at_utc",
                table: "service_requests",
                columns: new[] { "department_id", "status", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_request_status_history_status_service_request_id_changed_at_~",
                table: "request_status_history",
                columns: new[] { "status", "service_request_id", "changed_at_utc" });

            migrationBuilder.Sql("""
                CREATE VIEW vw_department_performance AS
                SELECT
                    service_request.department_id,
                    COALESCE(department.name, 'Unassigned') AS department_name,
                    COUNT(*) AS total_requests,
                    SUM(service_request.status IN (1, 2, 3)) AS open_requests,
                    SUM(resolution.resolved_at_utc IS NOT NULL) AS completed_requests,
                    AVG(
                        CASE
                            WHEN resolution.resolved_at_utc IS NOT NULL
                                THEN TIMESTAMPDIFF(SECOND, service_request.created_at_utc, resolution.resolved_at_utc) / 3600.0
                        END) AS average_resolution_hours,
                    SUM(resolution.resolved_at_utc IS NOT NULL) AS sla_eligible_requests,
                    SUM(
                        CASE
                            WHEN resolution.resolved_at_utc IS NOT NULL
                                AND TIMESTAMPDIFF(SECOND, service_request.created_at_utc, resolution.resolved_at_utc) <=
                                    CASE service_request.priority
                                        WHEN 4 THEN 14400
                                        WHEN 3 THEN 28800
                                        WHEN 2 THEN 259200
                                        WHEN 1 THEN 432000
                                    END
                                THEN 1
                            ELSE 0
                        END) AS sla_met_requests
                FROM service_requests AS service_request
                LEFT JOIN departments AS department ON department.id = service_request.department_id
                LEFT JOIN (
                    SELECT service_request_id, MIN(changed_at_utc) AS resolved_at_utc
                    FROM request_status_history
                    WHERE status IN (4, 5)
                    GROUP BY service_request_id
                ) AS resolution ON resolution.service_request_id = service_request.id
                GROUP BY service_request.department_id, department.name
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_department_performance");

            migrationBuilder.DropIndex(
                name: "IX_service_requests_department_id_status_created_at_utc",
                table: "service_requests");

            // The provider-generated truncated MySQL index name ends in '~', which requires quoting in DROP INDEX.
            migrationBuilder.Sql("DROP INDEX `IX_request_status_history_status_service_request_id_changed_at_~` ON `request_status_history`");
        }
    }
}

#pragma warning restore IDE0161
#pragma warning restore CA1861
