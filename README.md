# OperationsHub

OperationsHub is an internal service-request and workflow-management application for submitting, assigning, tracking, and auditing operational work. It is a portfolio project designed to demonstrate senior-level, practical engineering with C#, ASP.NET Core, Blazor, relational data, APIs, security, testing, Docker, and technical documentation.

The project favors a complete modular monolith over microservices or speculative abstractions.

## Current status

**Milestone 4 — MySQL demonstration and interview-ready MVP complete.**

The solution has a MySQL-backed EF Core context, ASP.NET Core Identity, Development-only demo identities, administrator-only reference-data management, and a complete service-request workflow. Requesters can submit, view, and edit permitted requests; technicians work assigned requests; managers and administrators assign and oversee all requests. Assignment, status, comments, and audit history are retained. Managers and administrators also have an open-request summary backed by a MySQL view. Assignment uses a transactional stored procedure and all versioned request mutations detect stale edits.

## Technology

- .NET 10 and C#
- ASP.NET Core and Blazor Web App
- Global Interactive Server rendering
- MySQL 8.4 for local development
- Entity Framework Core and selected handwritten SQL, beginning in Milestone 1
- ASP.NET Core Identity, beginning in Milestone 1
- xUnit
- Docker Compose

## Architecture

OperationsHub is split into four production projects:

```text
Web -> Infrastructure -> Application -> Domain
  \--------------------> Application
```

- **Domain** owns business entities and invariants.
- **Application** owns use cases, validation, DTOs, and contracts.
- **Infrastructure** implements database and external concerns.
- **Web** hosts the Blazor UI, REST API, middleware, and dependency composition.

See [docs/architecture.md](docs/architecture.md) and the records in [docs/decisions](docs/decisions).

## Prerequisites

- Linux, macOS, Windows, or WSL
- [.NET SDK 10.0.302](https://dotnet.microsoft.com/download/dotnet/10.0), or a compatible later 10.0 patch selected by `global.json`
- Docker Engine with the Compose plugin
- Git

Use `eng\dotnet.cmd` from native Windows PowerShell and `eng/dotnet.sh` from Bash. The Windows entry point invokes `eng\dotnet.ps1` with a process-scoped execution-policy bypass, so it works without changing machine or user policy. Each wrapper uses the platform-appropriate repository-local `.dotnet` SDK when one exists and otherwise delegates to `dotnet` on `PATH`. Both wrappers isolate CLI/package caches inside the repository and use the committed `NuGet.Config`; the PowerShell wrapper also normalizes duplicate `Path`/`PATH` variables that some sandboxed Windows environments provide.

## Local setup

### 1. Initialize the repository and local environment

Run these commands once when setting up a native Windows development machine:

```powershell
git clone git@github.com:mikefrawth/operations-hub.git
cd operations-hub
Copy-Item .env.example .env
docker compose up -d mysql
docker compose ps
.\eng\dotnet.cmd restore
```

The copied `.env` file contains local-development settings for Docker Compose and is ignored by Git. The web host's committed Development connection string matches the example defaults and disables TLS only for the loopback Docker connection. If you change the database user, password, port, or database name in `.env`, also provide a matching `ConnectionStrings__OperationsHub` environment variable when running the web host. `docker compose ps` shows whether MySQL has reached a healthy state. Restore downloads the NuGet dependencies required by the solution.

### 2. Build and run normally

For a normal local startup after the one-time setup:

```powershell
docker compose up -d mysql
.\eng\dotnet.cmd build --no-restore
.\eng\dotnet.cmd run --project src/OperationsHub.Web
```

Open `http://localhost:5090`. Stop the application with <kbd>Ctrl</kbd>+<kbd>C</kbd>.

Run `.\eng\dotnet.cmd restore` again after pulling changes that modify project files or NuGet dependencies. The `--no-restore` build option keeps the normal build fast by using dependencies that were already restored.

### 3. Develop with automatic rebuilds

Use `dotnet watch` while actively changing C# or Razor files:

```powershell
docker compose up -d mysql
.\eng\dotnet.cmd watch --project src/OperationsHub.Web
```

The watch process monitors supported source files, rebuilds the affected project, and refreshes or restarts the application when changes are detected. Keep it running during development and stop it with <kbd>Ctrl</kbd>+<kbd>C</kbd>. Open `http://localhost:5090` after the application reports that it is listening.

Run either the normal `run` command or the `watch` command at one time. Both use port `5090`, so the second process will report that the address is already in use.

### Optional HTTPS profile

The default development profile intentionally uses HTTP so it works cleanly across WSL and the Windows host browser. To use the HTTPS development profile, run:

```powershell
.\eng\dotnet.cmd run --project src/OperationsHub.Web --launch-profile https
```

The HTTPS profile listens on `https://localhost:7090` and also exposes `http://localhost:5090`. The committed values in `.env.example` are isolated-development examples only.

## Docker

MySQL listens on host port `3307` by default to avoid a common conflict with a host MySQL instance on `3306`.

```bash
docker compose config
docker compose up -d mysql
docker compose ps
docker compose logs mysql
docker compose down
```

To remove the local database volume and all of its data:

```bash
docker compose down --volumes
```

This reset is destructive and intended only for disposable local development data.

## Build, test, and format

```powershell
.\eng\dotnet.cmd restore
.\eng\dotnet.cmd build --no-restore
.\eng\dotnet.cmd test --no-build
.\eng\dotnet.cmd format --verify-no-changes --no-restore
```

Warnings and recommended analyzer findings fail the build.

On Linux, macOS, or WSL, replace `.\eng\dotnet.cmd` with `./eng/dotnet.sh`.

### Verified Windows-native commands

The following commands completed successfully in native Windows PowerShell on 2026-07-29. In Codex, the wrapper detects `CODEX_CI` or `CODEX_THREAD_ID`, disables build servers, and uses one MSBuild node to avoid sandbox IPC limitations.

```powershell
.\eng\dotnet.cmd restore OperationsHub.sln --verbosity minimal
.\eng\dotnet.cmd build OperationsHub.sln --no-restore --verbosity minimal
.\eng\dotnet.cmd test OperationsHub.sln --no-build --no-restore --verbosity minimal
.\eng\dotnet.cmd format OperationsHub.sln --verify-no-changes --no-restore --verbosity minimal
docker compose config
docker compose up -d mysql
docker compose ps
```

The sandboxed build completed with zero warnings and errors, all 21 sandboxed tests passed, formatting required no further changes, MySQL was healthy, and the rendered home page loaded at `http://localhost:5090` without browser console errors.

## Database migrations and seed data

Restore the repository-pinned EF tool, then apply all migrations:

```powershell
.\eng\dotnet.cmd tool restore
.\eng\dotnet.cmd tool run dotnet-ef database update `
  --project src/OperationsHub.Infrastructure `
  --startup-project src/OperationsHub.Web
```

The remediation migration removes role assignments and disables the fixed demo identities that the initial migration historically created. The web host recreates their password and role assignments only when it starts in the Development environment. See [docs/database.md](docs/database.md) for the schema, ER diagram, migration behavior, and seed data.

## Demo accounts

Development-only Requester, Technician, Manager, and Administrator accounts are created at Development startup with password `OperationsHub!2026`. They have lockout enabled and are never part of the current EF model seed. See [docs/database.md](docs/database.md) for their email addresses. These public credentials are suitable only for an isolated local environment.

## API antiforgery

The reference-data API uses the same cookie authentication as the Blazor UI. After signing in, a non-browser client must retain the authentication and antiforgery cookies, request `GET /api/antiforgery`, and send the returned request token in the returned header name for every `POST` or `PUT` request. Missing or invalid tokens are rejected before endpoint code runs.

## Feature status

| Capability | Status |
| --- | --- |
| Modular solution and Blazor host | Implemented in Milestone 0 |
| Local MySQL infrastructure | Implemented in Milestone 0 |
| Identity and demo users | Implemented in Milestone 1 |
| Department and request-type administration | Implemented in Milestone 2 |
| Service-request workflow and REST API | Implemented in Milestone 3 |
| Reporting view, stored procedure, and concurrency | Implemented in Milestone 4 |
| Engineering hardening | In progress in Milestone 5 |
| CI/CD | Planned for Milestone 6 |

See [docs/roadmap.md](docs/roadmap.md) for the complete sequence.

## Screenshots

Screenshots will be added after visual polish. The current UI is intentionally restrained and includes the service-request workflow plus the manager/administrator open-request summary.

## Deployment

Deployment-provider selection and production containerization are deferred to Milestone 6. Production configuration must come from the hosting platform through environment-based secrets; no production credentials belong in this repository.

## Known limitations

- The development sign-in screen uses an antiforgery-protected HTTP form and supports the Development-only demo accounts. Sign-in attempts are limited per remote IP, accounts lock for 15 minutes after five failed attempts, and registration, password recovery, multifactor authentication, and production identity-provider integration are deferred.
- Managers and administrators can assign requests only to active technicians selected from the technician directory.
- Development startup creates a deterministic open service request so the service-request and open-request-summary pages have populated-data coverage for local demonstrations.
- The unauthenticated `/health` endpoint checks database connectivity. The host writes structured JSON logs and returns safe error responses through centralized exception handling.
- The Compose stack contains MySQL only; application containerization is deferred until the web/database integration is reliable.
- Integration tests cover migrations, Development-only identity initialization, security endpoint metadata, reference-data administration, and the MySQL reporting/transaction/concurrency workflow.

## License

OperationsHub is available under the [MIT License](LICENSE).
