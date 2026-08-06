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

    -- Lock the request so version validation and all three writes share one atomic outcome.
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
    -- Persisted ServiceRequestStatus values: Resolved = 4, Closed = 5.
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
END;
