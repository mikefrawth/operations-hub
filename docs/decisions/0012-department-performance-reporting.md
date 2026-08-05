# Department performance reporting

## Context

Milestone 7 needs a report that demonstrates department volume, resolution time, and SLA performance without making a BI platform or a hosted environment a required dependency. The persisted request status is current state only, while the append-only status history records when a request first reached a terminal state.

## Decision

Use the migration-owned `vw_department_performance` MySQL view as the stable all-time reporting read model. It groups requests by their historical department, includes an `Unassigned` group, and uses the first `Resolved` or `Closed` history event as the completion instant. The report defines fixed targets: Critical 4 hours, High 8 hours, Normal 72 hours, and Low 120 hours. Managers and administrators receive the same report through the Blazor page, authenticated JSON API, and CSV export.

## Consequences

The report remains simple, reproducible, and usable by Power BI through its CSV export or an authenticated local/API workflow. It does not provide configurable SLA policies, reporting date windows, historical department reassignment, or a hosted Power BI gateway; those need real operational requirements before being added. The view's aggregation is supported by department/status and terminal-status indexes, but large-data performance should be measured before adding materialized summaries or separate analytics storage.

## Status

Accepted (2026-08-05).
