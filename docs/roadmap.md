# Delivery roadmap

The project advances through verified increments. A milestone is complete only when its implementation, authorization/validation where applicable, tests, documentation, and build are all current.

## Milestone 0 — repository foundation

Status: **complete (2026-07-28)**

- .NET 10 solution with Domain, Application, Infrastructure, Web, UnitTests, and IntegrationTests
- Enforced project dependency direction
- Blazor Web App with global Interactive Server rendering
- Repository-wide build, formatting, warning, and secret-exclusion policy
- Root and required directory-level `AGENTS.md` files
- MySQL 8.4 Docker Compose service on host port 3307
- `.env.example`, named volume, and health check
- README, architecture/database foundations, and decision records
- MIT license and foundation architecture smoke tests

Exit gate passed:

- Solution restore completed.
- All six projects built with zero warnings and errors.
- Four foundation tests passed.
- Formatting verification passed.
- Compose configuration rendered successfully.
- MySQL 8.4 started on host port 3307 and reported healthy.
- A read-only database query confirmed the `operationshub` database, `utf8mb4`, and `utf8mb4_0900_ai_ci`.
- The Blazor host started and returned the expected landing-page content over loopback HTTP.

## Milestone 1 — database and identity foundation

Status: **complete (2026-07-28)**

- MySQL connectivity and EF Core DbContext
- ASP.NET Core Identity
- Core entities and explicit entity configurations
- Initial migration and development seed data
- Requester, Technician, Manager, and Administrator demo users
- Synchronized Mermaid ER diagram and expanded database documentation
- MySQL migration/connectivity integration test

Exit gate passed:

- EF Core 10, ASP.NET Core Identity, and MySQL Connector/NET were restored.
- The initial migration was generated and reviewed against MySQL 8.4.
- The migration applied successfully to the local Compose database with seeded roles, users, departments, and request types.
- The model has no pending changes after migration generation.

## Milestone 2 — reference-data administration

Status: **complete and security-reviewed (2026-07-29)**

- Department create, list, edit, and deactivate workflows
- Request-type create, list, edit, and deactivate workflows
- Administrator authorization and server-side validation
- Blazor administration pages and API read endpoints
- Unit and integration coverage
- Development-only runtime demo identities; schema migrations leave public demo credentials disabled
- Sign-in rate limiting, failed-attempt lockout, explicit cookie protections, and defensive response headers
- Antiforgery validation on every cookie-authenticated mutation endpoint
- Security metadata coverage for authentication and reference-data endpoints

Exit gate passed:

- Administrator-only Blazor and API surfaces use the shared reference-data application service.
- Department and request-type names are trimmed, required, capped at 100 characters, and unique across active and inactive records.
- Request-type descriptions are optional and capped at 500 characters.
- Deactivation preserves records for historical relationships and removes them from active-only queries.
- Unit coverage verifies validation, duplicate rejection, and deactivation; the MySQL integration test verifies create, list, and deactivation inside a rolled-back transaction.
- The follow-up review verified that production configuration requires an externally supplied connection string, migration-only deployments do not leave usable demo credentials, API `401`/`403` responses remain API responses, and security metadata is present on protected endpoints.

## Milestone 3 — service-request workflow

Status: **complete (2026-07-29)**

- Request create, detail, permitted edit, close, pagination, search, and filtering
- Assignment/reassignment and assignment history
- Explicit status transitions and status history
- Chronological comments and meaningful audit events
- Role-specific request views and selected REST endpoints
- Unit and integration coverage for rules and authorization

Exit gate passed:

- The authenticated workflow UI lets requesters submit and edit permitted requests, provides role-scoped request lists, and presents request detail, comments, status history, and assignment history.
- The same application service backs the UI and protected REST endpoints for request creation, detail, edit, assignment, status, comments, search, filtering, and pagination.
- Requesters are limited to their own requests; technicians to their assignments; managers and administrators can view all requests and assign work.
- Request mutations require antiforgery validation and are covered by endpoint metadata tests; domain tests cover permitted and rejected lifecycle transitions.

## Milestone 4 — MySQL demonstration and interview-ready MVP

Status: **complete (2026-07-29)**

- `vw_open_request_summary`
- `sp_assign_request`
- Explicit transactional workflow and rollback coverage
- Handwritten parameterized SQL
- Optimistic concurrency and conflict response coverage
- Query indexes and `EXPLAIN` documentation

Exit gate passed:

- Managers and administrators can view the open-request summary backed by a MySQL view.
- Assignment runs through a parameterized stored-procedure call that locks the request, increments its version, and commits request, assignment-history, and audit writes together.
- Request updates, status changes, and assignments return a conflict result when their submitted version is stale.
- MySQL integration tests cover successful procedure writes, stale-version rollback, the reporting view, and the application conflict result.

## Milestone 5 — engineering hardening

Status: **in progress (2026-07-29)**

- Structured logging, centralized exception handling, Problem Details, and health checks
- Security, accessibility, performance, and test-coverage review
- Administrator reactivation workflows for inactive departments and request types, preserving the soft-deactivation audit/history model
- Seeded portfolio scenario and UI verification that populated service-request and open-request-summary views render their data for each permitted role
- Screenshots and implementation-driven cleanup

## Milestone 6 — CI/CD and deployment

- Production Dockerfile and CI pipeline
- Build, test, publish, deployment, smoke-test, and rollback stages
- Hosted application/MySQL configuration and environment-based secrets
- Deployment documentation

## Milestone 7 — reporting and Power BI readiness

- Department performance, resolution time, volume, and SLA reporting
- Stable reporting views
- CSV export or documented Power BI connection
- Query and index review

## Milestone 8 — advisory request classification

- `IRequestClassificationService`
- Deterministic rules and optional AI-backed implementation
- Suggested category, priority, and concise summary with human review
- Graceful fallback, offline tests, and no committed keys

## Milestone 9 — optional expansion

Potential work logs, SLA policies, attachments, internal comments, notifications, background jobs, audit search, dashboards, imports, SharePoint design, and responsive refinement. None begins before the MVP is complete and stable.
