using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable IDE0161 // Keep the EF-generated block-scoped migration namespace.

namespace OperationsHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HardenAssignmentProcedure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                    DECLARE v_active_technician_id VARCHAR(255) DEFAULT NULL;

                    -- Ensure pooled connections can never retain a failed assignment transaction.
                    DECLARE EXIT HANDLER FOR SQLEXCEPTION
                    BEGIN
                        ROLLBACK;
                        RESIGNAL;
                    END;

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
                        -- Lock Identity eligibility until the request and history writes commit.
                        SELECT app_user.Id
                        INTO v_active_technician_id
                        FROM AspNetUsers AS app_user
                        INNER JOIN AspNetUserRoles AS user_role ON user_role.UserId = app_user.Id
                        INNER JOIN AspNetRoles AS app_role ON app_role.Id = user_role.RoleId
                        WHERE app_user.Id = p_assignee_id
                          AND app_role.Name = 'Technician'
                          AND (app_user.LockoutEnabled = 0 OR app_user.LockoutEnd IS NULL OR app_user.LockoutEnd <= p_assigned_at_utc)
                        LIMIT 1
                        FOR UPDATE;

                        IF v_active_technician_id IS NULL THEN
                            ROLLBACK;
                            SELECT 'invalid_assignee' AS outcome, v_current_version AS version;
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
                    END IF;
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restore the previous procedure definition without recreating any user data.
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
        }
    }
}

#pragma warning restore IDE0161
