# Database design

## Current state

Milestone 0 provides a MySQL 8.4 development container but intentionally does not define an application schema. Entity mappings, the initial migration, Identity tables, seed data, and the synchronized Mermaid ER diagram are Milestone 1 deliverables.

## Planned entity overview

The initial model will include ApplicationUser/Identity roles, Department, RequestType, ServiceRequest, RequestAssignment, RequestComment, RequestStatusHistory, WorkLog, and AuditEvent. Fields and relationships will be added only when they support an implemented workflow.

Key design expectations:

- UTC timestamps throughout application persistence.
- Explicit maximum lengths, required fields, foreign keys, unique constraints, indexes, and delete behavior.
- Nullable current department and assignee until a request is assigned.
- An optimistic concurrency token on ServiceRequest.
- Append-oriented assignment, status, and audit history.
- Deactivation rather than physical deletion for referenced departments and request types.
- No cascade behavior that can erase operational or audit history.

## Index strategy

Indexes will be driven by implemented list filters and relationship access. The expected starting points are request number, status, priority, request type, requester, department, assignee, created date, and foreign keys used in history lookups. Composite index order will follow observed query predicates and be validated with MySQL `EXPLAIN`; the project will not create every possible index speculatively.

## Views and stored procedures

Milestone 4 will add:

- `vw_open_request_summary` for joined operational reporting, including request age.
- `sp_assign_request` for a deliberately selected atomic assignment/history demonstration.

Ordinary CRUD remains in EF Core. Procedure calls and raw SQL will be parameterized, cancellation-aware, and covered by MySQL integration tests.

## Migration strategy

EF Core migrations in Infrastructure will be the source of truth for application schema evolution. Each migration must:

1. Match the domain/persistence model and updated ER diagram.
2. Be reviewed for data loss, constraint behavior, provider-specific SQL, and rollback implications.
3. Be applied to a disposable MySQL container in integration testing.
4. Keep supplemental SQL under `/database` synchronized when a view or procedure is involved.

No migration exists in Milestone 0 because no schema has been implemented.

## Transaction strategy

Application use cases define business transaction boundaries. Infrastructure will execute multi-write operations—such as changing current assignment while appending assignment and audit history—atomically. Transaction scope, isolation, locking, and retry behavior will be explicit where MySQL behavior matters. Tests will demonstrate rollback on failure.

## Concurrency strategy

ServiceRequest will use optimistic concurrency. Update commands will carry the client-observed token; a mismatch will produce a clear conflict response instead of silently overwriting another user’s work. The exact MySQL-compatible token representation will be selected and tested in Milestone 1/4 rather than assumed in advance.

## Local database

Compose maps host port `3307` to MySQL’s container port `3306`, uses `utf8mb4`, persists data in a named volume, and provides a health check. Values in `.env.example` are development-only and must not be reused in deployed environments.
