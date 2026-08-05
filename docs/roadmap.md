# Delivery roadmap

The project advances through verified increments. A milestone is complete only when its implementation, authorization/validation where applicable, tests, documentation, and build are all current.

Across every milestone, Azure and all other live-production hosting work is **Archived / not currently planned**. It was deferred to avoid ongoing cloud costs for a portfolio project and is not a prerequisite, next milestone, required deployment step, exit gate, or definition-of-done item. The active demonstration path is the complete local Docker Compose stack; a scheduled remote demo may temporarily expose only the local web port through a Cloudflare Quick Tunnel, with a recorded walkthrough kept as backup.

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
- The follow-up review verified that non-Development configuration requires an externally supplied connection string, migration-only runs do not leave usable demo credentials, API `401`/`403` responses remain API responses, and security metadata is present on protected endpoints.

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

Status: **complete (2026-08-05)**

- Structured logging, centralized exception handling, Problem Details, and health checks
- Security, accessibility, performance, and test-coverage review
- Administrator reactivation workflows for inactive departments and request types, preserving the soft-deactivation audit/history model
- Development-only administrator impersonation with eligible-user filtering, audit events, and a persistent return banner
- Seeded portfolio scenario and UI verification that populated service-request and open-request-summary views render their data for each permitted role
- Screenshots and implementation-driven cleanup

Exit gate passed:

- Structured JSON logging, centralized exception handling, safe API Problem Details, database-backed readiness, and process-only liveness remained covered by the assembled host.
- Security review confirmed server-side authorization, antiforgery metadata on cookie-authenticated mutations, sign-in throttling and lockout, secure cookie settings, safe return URLs, and defensive response headers.
- Accessibility cleanup added contextual action names, table captions, readable workflow-state labels, and client validation summaries while preserving page titles, headings, labels, empty states, and the skip link.
- Request lists now expose the existing bounded search, status/priority filtering, and pagination behavior in the Blazor frontend.
- Development startup creates an assigned, in-progress scenario with comments and complete history; browser checks verified populated Requester, Technician, Manager, and Administrator views.
- Identity-backed participant display names replace internal identifiers in request history and reporting while remaining behind an Application contract.
- The real-MySQL scenario test verifies all four role views, reporting, participant lookup, and audit/history data; all 30 tests passed.
- Restore, build, test, and formatting verification completed successfully, and portfolio screenshots were captured from the verified application.

See [engineering-hardening-review.md](engineering-hardening-review.md) for the review evidence and stated limitations.

## Milestone 6 — local container delivery and continuous verification

Status: **local scope implemented; live hosting Archived / not currently planned (2026-08-03)**

Implemented local-delivery scope:

- Dockerfile implemented with pinned build/runtime images, a non-root runtime, and a health check
- Full local Docker Compose stack implemented for the web host, MySQL, health-based startup, and persistent Data Protection keys
- Explicit one-shot migration mode implemented without Development demo initialization
- GitHub Actions quality gate: restore, build, test, and formatting verification for pull requests and `main`
- Docker Compose build, readiness/liveness, public-route, static-asset, and fresh-database migration-only smoke test with demo-data isolation verification
- Successful `main` builds publish immutable SHA-tagged build artifacts to GitHub Container Registry; publication is not a live deployment
- Supported portfolio demonstration documented for local Docker Compose, with an optional scheduled Cloudflare Quick Tunnel and recorded-walkthrough backup

Archived hosted scope — retained only as a possible future design:

- Azure resource provisioning, Azure Container Registry, Container Apps, and Azure Database for MySQL
- Private VNet networking and Azure Files or another protected shared Data Protection key store
- GitHub OIDC deployment, live-environment secrets, hosted smoke tests, and production rollback
- Any equivalent provider-specific public hosting environment

This archived scope was deferred to avoid ongoing cloud costs for a portfolio project. It is neither pending Milestone 6 work nor required for milestone completion. No workflow may require Azure credentials, ACR, or a live Azure environment while this decision remains active.

Container increment verification:

- The pinned multi-stage image restored and published the web host successfully.
- The web and MySQL Compose services both reached healthy status.
- The containerized landing page and both liveness/readiness probes returned HTTP `200`.
- Migration-only mode completed against MySQL and exited successfully.
- The runtime used UID/GID `1654`, could not write to `/app`, and wrote its Data Protection key only to the mounted key volume.

## Milestone 7 — reporting and Power BI readiness

Status: **complete (2026-08-05)**

- Department performance, resolution time, volume, and SLA reporting
- Stable reporting views
- CSV export and documented Power BI connection
- Query and index review

Exit gate passed:

- Managers and administrators can view department volume, open work, completed work, average resolution time, and SLA compliance in the Blazor UI at `/reports/department-performance`.
- `vw_department_performance` is migration-owned and provides the stable MySQL read model; its outcome measures use the first `Resolved` or `Closed` history event for each request.
- The browser-authenticated API exposes the report at `GET /api/reports/department-performance` and provides `GET /api/reports/department-performance.csv` for CSV/Power BI import.
- SLA targets are explicit and consistent across the view, UI, export, and documentation: Critical 4 hours, High 8 hours, Normal 72 hours, and Low 120 hours.
- The MySQL integration test verifies calculated volume, resolution, and SLA results, while indexes support department/status aggregation and terminal-status lookup.

## Milestone 8 — advisory request classification

Status: **complete (2026-08-05)**

- `IRequestClassificationService`
- Deterministic rules and optional AI-backed implementation
- Suggested category, priority, and concise summary with human review
- Graceful fallback, offline tests, and no committed keys

Exit gate passed:

- Requesters can request a suggestion in the Blazor submission form, review its category, priority, and concise summary, then explicitly apply the category and priority before submitting; no suggestion automatically mutates a request.
- `IRequestClassificationService` obtains active request types from the server-side reference-data contract and enforces requester-only access.
- The deterministic implementation is always available and offline-testable. An optional OpenAI Responses API adapter is enabled only with an externally supplied API key, uses structured output, and returns the deterministic suggestion if configuration, transport, or output validation fails.
- No key is committed. Use `RequestClassification__OpenAi__ApiKey` as a user secret or environment variable; `RequestClassification__OpenAi__Model` and `RequestClassification__OpenAi__TimeoutSeconds` are optional overrides.

## Milestone 9 — optional expansion

Potential work logs, SLA policies, attachments, internal comments, notifications, background jobs, audit search, dashboards, imports, SharePoint design, and responsive refinement. None begins before the MVP is complete and stable.
