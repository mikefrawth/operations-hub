CREATE OR REPLACE VIEW vw_department_performance AS
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
                    -- Persisted priorities: Critical = 4, High = 3, Normal = 2, Low = 1.
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
    -- Persisted terminal statuses: Resolved = 4, Closed = 5.
    WHERE status IN (4, 5)
    GROUP BY service_request_id
) AS resolution ON resolution.service_request_id = service_request.id
GROUP BY service_request.department_id, department.name;
