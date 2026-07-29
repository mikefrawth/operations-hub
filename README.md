# OperationsHub

OperationsHub is an internal service-request and workflow-management application for submitting, assigning, tracking, and auditing operational work. It is a portfolio project designed to demonstrate senior-level, practical engineering with C#, ASP.NET Core, Blazor, relational data, APIs, security, testing, Docker, and technical documentation.

The project favors a complete modular monolith over microservices or speculative abstractions.

## Current status

**Milestone 0 — repository foundation complete.**

The solution structure, project boundaries, Blazor host, repository policy, MySQL development container, smoke tests, and foundational documentation are present. Database connectivity, Identity, entities, migrations, and demo users are intentionally deferred to Milestone 1.

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

`eng/dotnet.sh` uses a repository-local `.dotnet` SDK when one exists; otherwise it delegates to `dotnet` on `PATH`.

## Local setup

### 1. Initialize the repository and local environment

Run these commands once when setting up a new development machine or WSL distribution:

```bash
git clone git@github.com:mikefrawth/operations-hub.git
cd operations-hub
cp .env.example .env
docker compose up -d mysql
docker compose ps
./eng/dotnet.sh restore
```

The copied `.env` file contains local-development settings and is ignored by Git. `docker compose ps` shows whether MySQL has reached a healthy state. Restore downloads the NuGet dependencies required by the solution.

### 2. Build and run normally

For a normal local startup after the one-time setup:

```bash
docker compose up -d mysql
./eng/dotnet.sh build --no-restore
./eng/dotnet.sh run --project src/OperationsHub.Web
```

Open `http://localhost:5090`. Stop the application with <kbd>Ctrl</kbd>+<kbd>C</kbd>.

Run `./eng/dotnet.sh restore` again after pulling changes that modify project files or NuGet dependencies. The `--no-restore` build option keeps the normal build fast by using dependencies that were already restored.

### 3. Develop with automatic rebuilds

Use `dotnet watch` while actively changing C# or Razor files:

```bash
docker compose up -d mysql
./eng/dotnet.sh watch --project src/OperationsHub.Web
```

The watch process monitors supported source files, rebuilds the affected project, and refreshes or restarts the application when changes are detected. Keep it running during development and stop it with <kbd>Ctrl</kbd>+<kbd>C</kbd>. Open `http://localhost:5090` after the application reports that it is listening.

Run either the normal `run` command or the `watch` command at one time. Both use port `5090`, so the second process will report that the address is already in use.

### Optional HTTPS profile

The default development profile intentionally uses HTTP so it works cleanly across WSL and the Windows host browser. To use the HTTPS development profile, run:

```bash
./eng/dotnet.sh run --project src/OperationsHub.Web --launch-profile https
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

```bash
./eng/dotnet.sh restore
./eng/dotnet.sh build --no-restore
./eng/dotnet.sh test --no-build
./eng/dotnet.sh format --verify-no-changes --no-restore
```

Warnings and recommended analyzer findings fail the build.

### Verified Milestone 0 commands

The following commands completed successfully on Fedora 44 under WSL on 2026-07-28. Single-process build flags were used because the execution sandbox restricts some local MSBuild process communication; they are safe but generally unnecessary on a normal workstation.

```bash
./eng/dotnet.sh restore OperationsHub.sln --disable-parallel -m:1 --verbosity minimal
./eng/dotnet.sh build OperationsHub.sln --no-restore --disable-build-servers \
  -p:BuildInParallel=false -p:UseSharedCompilation=false -m:1 --verbosity minimal
./eng/dotnet.sh test OperationsHub.sln --no-build --no-restore --disable-build-servers \
  -p:BuildInParallel=false -p:UseSharedCompilation=false -m:1 --verbosity minimal
./eng/dotnet.sh format OperationsHub.sln --verify-no-changes --no-restore --verbosity minimal
docker compose config
docker compose up -d mysql
docker inspect --format '{{.State.Health.Status}}' operationshub-mysql-1
```

The build completed with zero warnings and errors, all four tests passed, formatting required no further changes, the Blazor landing page returned the expected content over loopback HTTP, and MySQL reported healthy with the expected database, `utf8mb4` character set, and `utf8mb4_0900_ai_ci` collation.

## Database migrations and seed data

Database integration begins in Milestone 1. The planned commands, after the EF Core tool and DbContext are added, are:

```bash
./eng/dotnet.sh ef migrations add <MigrationName> \
  --project src/OperationsHub.Infrastructure \
  --startup-project src/OperationsHub.Web \
  --output-dir Persistence/Migrations
./eng/dotnet.sh ef database update \
  --project src/OperationsHub.Infrastructure \
  --startup-project src/OperationsHub.Web
```

These commands are documented as planned and are not yet available in Milestone 0.

## Demo accounts

No accounts exist in Milestone 0. Development-only Requester, Technician, Manager, and Administrator accounts will be seeded and documented in Milestone 1. Their credentials will never be suitable for production.

## Feature status

| Capability | Status |
| --- | --- |
| Modular solution and Blazor host | Implemented in Milestone 0 |
| Local MySQL infrastructure | Implemented in Milestone 0 |
| Identity and demo users | Planned for Milestone 1 |
| Department and request-type administration | Planned for Milestone 2 |
| Service-request workflow and REST API | Planned for Milestone 3 |
| Reporting view, stored procedure, and concurrency | Planned for Milestone 4 |
| Production hardening and CI/CD | Planned for Milestones 5–6 |

See [docs/roadmap.md](docs/roadmap.md) for the complete sequence.

## Screenshots

Screenshots will be added after the interview-ready workflow and visual polish exist. The Milestone 0 UI is intentionally a restrained application shell.

## Deployment

Deployment-provider selection and production containerization are deferred to Milestone 6. Production configuration must come from the hosting platform through environment-based secrets; no production credentials belong in this repository.

## Known limitations

- The web application does not yet connect to MySQL.
- Authentication, authorization, APIs, business entities, migrations, and seed data are not yet implemented.
- The Compose stack contains MySQL only; application containerization is deferred until the web/database integration is reliable.
- Integration tests currently verify assembly and architecture foundations, not database behavior.

## License

OperationsHub is available under the [MIT License](LICENSE).
