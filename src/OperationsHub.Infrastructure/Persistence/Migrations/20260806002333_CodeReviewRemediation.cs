using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1861 // EF migration operations use inline key and column arrays.
#pragma warning disable IDE0161 // Keep the EF-generated block-scoped migration namespace.

namespace OperationsHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CodeReviewRemediation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ServiceRequestStatus is persisted as integers: Resolved = 4 and Closed = 5.
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_assign_request");
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
                    ELSEIF v_current_status = 5 THEN
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

            // The open view must include New = 1, InProgress = 2, and OnHold = 3.
            migrationBuilder.Sql("""
                CREATE OR REPLACE VIEW vw_open_request_summary AS
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
                WHERE status IN (1, 2, 3)
                """);

            migrationBuilder.DropTable(
                name: "work_logs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "work_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false),
                    author_id = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "datetime(6)", precision: 6, nullable: false),
                    hours = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    note = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true),
                    service_request_id = table.Column<Guid>(type: "char(36)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_work_logs_AspNetUsers_author_id",
                        column: x => x.author_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_work_logs_service_requests_service_request_id",
                        column: x => x.service_request_id,
                        principalTable: "service_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_work_logs_author_id",
                table: "work_logs",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "IX_work_logs_service_request_id_created_at_utc",
                table: "work_logs",
                columns: new[] { "service_request_id", "created_at_utc" });

            // Restore the exact pre-remediation definitions when explicitly rolling this migration back.
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_assign_request");
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

            migrationBuilder.Sql("""
                CREATE OR REPLACE VIEW vw_open_request_summary AS
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
        }
    }
}

#pragma warning restore IDE0161
#pragma warning restore CA1861
