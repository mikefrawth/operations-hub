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

- MySQL connectivity and EF Core DbContext
- ASP.NET Core Identity
- Core entities and explicit entity configurations
- Initial migration and development seed data
- Requester, Technician, Manager, and Administrator demo users
- Synchronized Mermaid ER diagram and expanded database documentation
- MySQL migration/connectivity integration test

## Milestone 2 — reference-data administration

- Department create, list, edit, and deactivate workflows
- Request-type create, list, edit, and deactivate workflows
- Administrator authorization and server-side validation
- Blazor administration pages and API read endpoints
- Unit and integration coverage

## Milestone 3 — service-request workflow

- Request create, detail, permitted edit, close, pagination, search, and filtering
- Assignment/reassignment and assignment history
- Explicit status transitions and status history
- Chronological comments and meaningful audit events
- Role-specific request views and selected REST endpoints
- Unit and integration coverage for rules and authorization

## Milestone 4 — MySQL demonstration and interview-ready MVP

- `vw_open_request_summary`
- `sp_assign_request`
- Explicit transactional workflow and rollback coverage
- Handwritten parameterized SQL
- Optimistic concurrency and conflict response coverage
- Query indexes and `EXPLAIN` documentation

## Milestone 5 — engineering hardening

- Structured logging, centralized exception handling, Problem Details, and health checks
- Security, accessibility, performance, and test-coverage review
- Seeded portfolio scenario, screenshots, and implementation-driven cleanup

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
