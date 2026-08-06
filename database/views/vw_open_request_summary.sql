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
-- Persisted open statuses: New = 1, InProgress = 2, OnHold = 3.
WHERE status IN (1, 2, 3);
