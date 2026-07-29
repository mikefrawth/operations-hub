using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1861 // EF migration operations use inline key and column arrays.
#pragma warning disable IDE0161 // Keep the EF-generated block-scoped migration namespace.

namespace OperationsHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRequestReportingAndAssignmentProcedure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_service_requests_status_updated_at_utc",
                table: "service_requests",
                columns: new[] { "status", "updated_at_utc" });

            migrationBuilder.Sql("""
                CREATE VIEW vw_open_request_summary AS
                SELECT
                    id,
                    request_number,
                    title,
                    status,
                    priority,
                    requester_id,
                    assignee_id,
                    created_at_utc,
                    updated_at_utc,
                    TIMESTAMPDIFF(SECOND, created_at_utc, UTC_TIMESTAMP(6)) AS age_seconds
                FROM service_requests
                WHERE status IN (0, 1, 2)
                """);

            migrationBuilder.Sql("""
                CREATE PROCEDURE sp_assign_request(
                    IN p_request_id CHAR(36),
                    IN p_assignee_id VARCHAR(255),
                    IN p_actor_id VARCHAR(255),
                    IN p_expected_version INT UNSIGNED,
                    IN p_assigned_at_utc DATETIME(6))
                proc: BEGIN
                    DECLARE v_current_version INT UNSIGNED DEFAULT NULL;
                    DECLARE v_current_status INT DEFAULT NULL;

                    START TRANSACTION;

                    SELECT version, status
                    INTO v_current_version, v_current_status
                    FROM service_requests
                    WHERE id = p_request_id
                    FOR UPDATE;

                    IF v_current_version IS NULL THEN
                        ROLLBACK;
                        SELECT 'not_found' AS outcome, NULL AS version;
                    ELSEIF v_current_version <> p_expected_version THEN
                        ROLLBACK;
                        SELECT 'conflict' AS outcome, v_current_version AS version;
                    ELSEIF v_current_status = 4 THEN
                        ROLLBACK;
                        SELECT 'closed' AS outcome, v_current_version AS version;
                    ELSE
                        UPDATE service_requests
                        SET assignee_id = p_assignee_id,
                            updated_at_utc = p_assigned_at_utc,
                            version = version + 1
                        WHERE id = p_request_id;

                        INSERT INTO request_assignments (id, service_request_id, assignee_id, assigned_by_id, assigned_at_utc)
                        VALUES (UUID(), p_request_id, p_assignee_id, p_actor_id, p_assigned_at_utc);

                        INSERT INTO audit_events (id, service_request_id, event_type, actor_id, details, occurred_at_utc)
                        VALUES (UUID(), p_request_id, 'request-assigned', p_actor_id, p_assignee_id, p_assigned_at_utc);

                        COMMIT;
                        SELECT 'success' AS outcome, v_current_version + 1 AS version;
                    END IF;
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_assign_request");
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_open_request_summary");

            migrationBuilder.DropIndex(
                name: "IX_service_requests_status_updated_at_utc",
                table: "service_requests");
        }
    }
}
