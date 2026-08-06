# OperationsHub

[![Validate and publish](https://github.com/mikefrawth/operations-hub/actions/workflows/ci.yml/badge.svg)](https://github.com/mikefrawth/operations-hub/actions/workflows/ci.yml)

OperationsHub is a production-minded portfolio application for teams that need to submit, assign, progress, report on, and audit internal service work without losing ownership or history across email and spreadsheets. It uses a pragmatic modular monolith so request workflow, authorization, and audit writes remain understandable and transactionally consistent.

## Architecture at a glance

```mermaid
flowchart LR
    Browser["Blazor UI / REST client"] --> Web["Web<br/>ASP.NET Core + Blazor"]
    Web --> Application["Application<br/>use cases + DTOs"]
    Web --> Infrastructure["Infrastructure<br/>EF Core + adapters"]
    Infrastructure --> Application
    Application --> Domain["Domain<br/>state + invariants"]
    Infrastructure --> Domain
    Infrastructure --> MySQL["MySQL 8.4<br/>tables + views + procedure"]
```

The source dependency direction is `Web → Application → Domain` and `Web → Infrastructure → Application/Domain`. See the [architecture guide](docs/architecture.md) and [decision records](docs/decisions).

## Engineering highlights

- **Modular monolith:** .NET 10, C#, ASP.NET Core, and a Blazor Web App with global Interactive Server rendering and explicit Domain/Application/Infrastructure/Web boundaries.
- **REST API and OpenAPI:** first-party ASP.NET Core OpenAPI JSON in Development, useful operation/response metadata, cookie-auth documentation, and no added Swagger UI.
- **Relational depth:** MySQL 8.4, EF Core migrations and routine persistence, parameterized SQL, reporting views, and a transactional assignment stored procedure.
- **Security and correctness:** ASP.NET Core Identity roles, server-side authorization, executed antiforgery validation, optimistic concurrency, soft deactivation, append-only workflow history, and auditing.
- **Verification and delivery:** deterministic unit tests, assembled-host and real-MySQL integration tests, GitHub Actions gates, explicit migration-only execution, and Docker Compose smoke tests.
- **Advisory AI:** optional external classification with disclosure, bounded inputs, rate limiting, human application of suggestions, and a deterministic offline fallback.

## Representative application screens

| Service-request workflow and participant history | MySQL-backed open-request reporting |
| --- | --- |
| ![Service request detail with workflow and participant history](docs/images/service-request-detail.png) | ![Open request summary viewed through administrator impersonation](docs/images/open-request-summary.png) |

## Fastest supported start

With Git and Docker running:

```powershell
git clone https://github.com/mikefrawth/operations-hub.git
cd operations-hub
docker compose up --build --detach
docker compose ps
```

Open `http://localhost:5090`. The default Compose values are suitable only for the isolated Development demo; `.env.example` documents optional local overrides. Stop with `docker compose down`.

## Three-minute demonstration

1. **0:00–0:30 — Establish health and scope.** Show `docker compose ps`, open `/health/ready`, and explain the four-project modular-monolith diagram above.
2. **0:30–1:15 — Follow the requester path.** Sign in as the Development Administrator, choose **Test as another user → Demo Requester**, open the seeded request, and point out role-scoped visibility, comments, assignment history, and status history. On **Requests**, show the advisory classifier and its deterministic fallback without submitting sensitive text.
3. **1:15–2:15 — Switch roles at a real security boundary.** Return through the persistent impersonation banner, test as **Demo Manager**, open **Open request summary**, and assign or progress a request. Explain that the server rechecks authorization, antiforgery state, technician eligibility, and the concurrency version; assignment commits through `sp_assign_request`.
4. **2:15–3:00 — Show reporting and verification.** Open **Department performance**, then `/openapi/v1.json`. Close on the GitHub Actions badge and the real-MySQL lifecycle test that exercises sign-in, antiforgery, create, update, assignment, status, comment, history, and audit persistence through HTTP.

**Current scope:** milestones 0–8 are complete. Azure and all other live-production hosting are **Archived / not currently planned** to avoid ongoing cloud costs; the supported complete demonstration is local Docker Compose.

## Optional native development workflow

Skip this entire section when running the application with Docker. It exists only for contributors who deliberately want to run the web process outside its container for `dotnet watch`, direct debugger integration, or repository test commands.

That optional workflow also requires:

- Linux, macOS, Windows, or WSL
- [.NET SDK 10.0.302](https://dotnet.microsoft.com/download/dotnet/10.0), or a compatible later 10.0 patch selected by `global.json`
- Docker Engine with the Compose plugin
- Git

Use `eng\dotnet.cmd` from native Windows PowerShell and `eng/dotnet.sh` from Bash. The Windows entry point invokes `eng\dotnet.ps1` with a process-scoped execution-policy bypass, so it works without changing machine or user policy. Each wrapper uses the platform-appropriate repository-local `.dotnet` SDK when one exists and otherwise delegates to `dotnet` on `PATH`. Both wrappers isolate CLI/package caches inside the repository and use the committed `NuGet.Config`; the PowerShell wrapper also normalizes duplicate `Path`/`PATH` variables that some sandboxed Windows environments provide.

### 1. Initialize the native toolchain

Run these commands once when choosing to run the web host directly:

```powershell
git clone https://github.com/mikefrawth/operations-hub.git
cd operations-hub
Copy-Item .env.example .env
docker compose up -d mysql
docker compose ps
.\eng\dotnet.cmd restore
```

The copied `.env` file contains local-development settings for Docker Compose and is ignored by Git. The web host's committed Development connection string matches the example defaults and disables TLS only for the loopback Docker connection. If you change the database user, password, port, or database name in `.env`, also provide a matching `ConnectionStrings__OperationsHub` environment variable when running the web host. `docker compose ps` shows whether MySQL has reached a healthy state. Restore downloads the NuGet dependencies required by the solution.

### 2. Build and run the web host natively

In this optional mode, Docker runs only MySQL and the .NET wrapper runs the web host:

```powershell
docker compose up -d mysql
.\eng\dotnet.cmd build --no-restore
.\eng\dotnet.cmd run --project src/OperationsHub.Web
```

Open `http://localhost:5090`. Stop the application with <kbd>Ctrl</kbd>+<kbd>C</kbd>.

Run `.\eng\dotnet.cmd restore` again after pulling changes that modify project files or NuGet dependencies. The `--no-restore` build option keeps the normal build fast by using dependencies that were already restored.

### 3. Develop natively with automatic rebuilds

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

## Docker operations

The default Compose project runs the complete application. The web container listens on host port `5090`, and MySQL listens on host port `3307` so native development and integration tests can reach the same database.

```bash
docker compose config
docker compose up --build --detach
docker compose ps
docker compose logs --follow web
docker compose logs mysql
docker compose down
```

Rebuild after changing application source or the Dockerfile:

```bash
docker compose up --build --detach web
```

The image health check verifies both process liveness and delivery of the Blazor framework script required for interactive buttons and forms.

If startup fails, confirm the Docker daemon is available with `docker info`, then inspect `docker compose ps` and `docker compose logs web`. The `web` service should remain `Up` and become `healthy`; a `Restarting` state means the web logs contain the startup failure. Restarting existing containers does not rebuild changed source, so use `docker compose up --build --detach web` after C#, Razor, CSS, project-file, or Dockerfile changes.

For native web development, start only MySQL with `docker compose up -d mysql`. To remove both local named volumes, including all database data and persisted local sign-in keys:

```bash
docker compose down --volumes
```

This reset is destructive and intended only for disposable local development data. See [docs/deployment.md](docs/deployment.md) for the local image contract, health endpoints, temporary demonstration workflow, and the clearly separated archive of the former hosted-production design.

## Contributor build, test, and format

These commands verify source changes outside the application container. They are not required to start the Docker Compose application.

```powershell
.\eng\dotnet.cmd restore
.\eng\dotnet.cmd build --no-restore
.\eng\dotnet.cmd test --no-build
.\eng\dotnet.cmd format --verify-no-changes --no-restore
```

Warnings and recommended analyzer findings fail the build.

On Linux, macOS, or WSL, replace `.\eng\dotnet.cmd` with `./eng/dotnet.sh`.

The integration test project uses real MySQL. For native test runs, start the disposable database with `docker compose up -d mysql` first; GitHub Actions provisions the same MySQL 8.4 service on port `3307` before running the solution tests. CI also exercises migration-only mode against a fresh database before Development startup and verifies that it does not create reusable demo credentials, demo role assignments, or the portfolio request.

### Verified Windows-native commands

The native .NET commands below were reverified successfully in Windows PowerShell on 2026-08-06. In Codex, the wrapper detects `CODEX_CI` or `CODEX_THREAD_ID`, disables build servers, and uses one MSBuild node to avoid sandbox IPC limitations.

```powershell
.\eng\dotnet.cmd restore OperationsHub.sln --verbosity minimal
.\eng\dotnet.cmd build OperationsHub.sln --no-restore --verbosity minimal
.\eng\dotnet.cmd test OperationsHub.sln --no-build --no-restore --verbosity minimal
.\eng\dotnet.cmd format OperationsHub.sln --verify-no-changes --no-restore --verbosity minimal
docker compose config
docker compose build web
docker compose up --detach
docker compose ps
docker compose run --rm web --migrate
```

The build completed with zero warnings and errors, all 62 tests passed (31 unit and 31 integration), and formatting required no further changes. The container sequence was reverified in an isolated Compose project: the web image built successfully; migration-only mode completed against a fresh database without creating reusable demo credentials, demo role assignments, or the portfolio request; both services reported healthy; `/health/live`, `/health/ready`, the landing page, the Blazor framework asset, and `/openapi/v1.json` returned HTTP `200`; and the web process ran as the image's unprivileged `app` user.

## Database migrations and seed data

Development startup applies migrations automatically. For native tooling, restore the repository-pinned EF tool and apply migrations with:

```powershell
.\eng\dotnet.cmd tool restore
.\eng\dotnet.cmd tool run dotnet-ef database update `
  --project src/OperationsHub.Infrastructure `
  --startup-project src/OperationsHub.Web
```

Fresh migration history never creates demo identities, password hashes, or user-role assignments. The retained remediation migration disables those credentials for databases that applied an earlier revision of the initial migration. The web host creates or restores the demo password and roles only when it starts in the Development environment. See [docs/database.md](docs/database.md) for the schema, ER diagram, migration behavior, and seed data.

The container image also provides a migration-only mode that does not start the HTTP server or initialize Development demo accounts:

```bash
docker compose run --rm web --migrate
```

CI verifies this mode against a fresh disposable Compose database before Development startup. The archived hosting design retained it as a possible one-shot schema migration mechanism, but no live release currently uses it.

## Demo accounts

Development-only Requester, Technician, Manager, and Administrator accounts are created at Development startup with password `OperationsHub!2026`. They have lockout enabled and are never part of the current EF model seed. See [docs/database.md](docs/database.md) for their email addresses. These public credentials are suitable only for an isolated local environment.

After signing in as the Administrator, open **Test as another user** in the navigation to assume a Requester, Technician, or Manager profile without entering another password. A yellow banner stays at the top of the screen, identifies the effective profile on every page, and returns to the original administrator in one click. Signing out from the side navigation during a test session ends the tested profile session and restores the administrator. Start and end transitions are audited. This feature is mapped and authorized only in Development.

## API and OpenAPI

The first-party ASP.NET Core OpenAPI document is available at `GET /openapi/v1.json` only in `Development`, including the supported Docker Compose demo. No interactive Swagger-style UI is installed. The JSON APIs use the Blazor UI's Identity cookie rather than bearer tokens or API keys. A client must complete the antiforgery-protected sign-in form, retain the authentication and antiforgery cookies, call `GET /api/antiforgery`, and send the returned token in the returned header for every JSON `POST` or `PUT`. Missing or invalid state is rejected before endpoint code runs. See the [API and OpenAPI guide](docs/api.md) for roles, status semantics, and the consumer flow.

## Feature status

| Capability | Status |
| --- | --- |
| Modular solution and Blazor host | Implemented in Milestone 0 |
| Local MySQL infrastructure | Implemented in Milestone 0 |
| Identity and demo users | Implemented in Milestone 1 |
| Department and request-type administration | Implemented in Milestone 2 |
| Service-request workflow and REST API | Implemented in Milestone 3 |
| First-party Development OpenAPI contract | Implemented in interview-readiness hardening |
| Reporting view, stored procedure, and concurrency | Implemented in Milestone 4 |
| Development administrator impersonation | Implemented in Milestone 5 |
| Engineering hardening | Completed in Milestone 5 |
| Container delivery | Dockerfile and full Compose stack implemented for Milestone 6 |
| CI and local container verification | Implemented in Milestone 6 |
| Department performance reporting and CSV export | Implemented in Milestone 7 |
| Advisory request classification | Implemented in Milestone 8 |
| Azure or other live-production hosting | **Archived / not currently planned** to avoid ongoing portfolio-project cloud costs |

See [docs/roadmap.md](docs/roadmap.md) for the complete sequence.

## Portfolio demonstration

Docker Compose is the supported way to run and demonstrate the complete application, including MySQL. There is no active Azure or other live hosted environment, and cloud provisioning is not a prerequisite, next milestone, deployment step, or definition-of-done item.

For a scheduled remote demonstration, start the local stack from the repository root:

```powershell
Copy-Item .env.example .env
docker compose up --build --detach
docker compose ps
```

Use `cp .env.example .env` for the first command on Linux, macOS, or WSL. Confirm that both services are healthy and that `http://localhost:5090/health/ready` succeeds. The default web port is `5090`, from the Compose mapping `${APP_PORT:-5090}:8080`.

With `cloudflared` installed, start a Quick Tunnel in a separate terminal:

```bash
cloudflared tunnel --url http://localhost:5090
```

If `APP_PORT` overrides the default, use the mapped host port in the same command:

```text
cloudflared tunnel --url http://localhost:<web-port>
```

Share the generated random `https://...trycloudflare.com` URL only with the scheduled attendees and only for the duration of the demonstration. A Quick Tunnel is not a production deployment: it has no uptime guarantee, its URL changes when restarted, and the demo laptop must remain awake with Docker Compose and `cloudflared` running. Do not publish the URL, expose `.env` or other sensitive configuration, or make the Development-only demo accounts available outside the controlled session. Keep a short recorded walkthrough as a backup.

Immediately after the demonstration, stop `cloudflared` with <kbd>Ctrl</kbd>+<kbd>C</kbd>, then stop the local stack:

```bash
docker compose down
```

The former Azure/live-production plan is preserved only as an archived future option in [docs/deployment.md](docs/deployment.md) and [decision 0011](docs/decisions/0011-local-portfolio-demonstration.md).

## Known limitations

- The development sign-in screen uses an antiforgery-protected HTTP form and supports the Development-only demo accounts. Sign-in attempts are limited per remote IP, accounts lock for 15 minutes after five failed attempts, and registration, password recovery, multifactor authentication, and a non-Development identity provider are outside the active portfolio scope.
- Administrator impersonation is Development-only and accepts only active accounts with exactly one Requester, Technician, or Manager role. A non-Development support impersonation path is intentionally not implemented.
- Managers and administrators can assign requests only to active technicians selected from the technician directory.
- Development startup creates a deterministic in-progress request assigned to the demo Technician, with comments, status history, assignment history, and audit events so every permitted role has a populated local demonstration path.
- The unauthenticated `/health` and `/health/ready` endpoints check database connectivity; `/health/live` checks only whether the web process can serve requests. The host writes structured JSON logs and returns safe error responses through centralized exception handling.
- Compose intentionally runs the web host in `Development` for disposable local demonstrations. Migration-only mode never initializes the public demo identities.
- Compose stores unencrypted Data Protection keys in a private local named volume for restart-stable demo sessions; this local configuration must not become a persistent public environment.
- Azure and other live-production hosting are Archived / not currently planned. The optional Quick Tunnel is temporary, has a changing URL and no uptime guarantee, and depends on the demo laptop remaining online.
- Integration tests cover migrations, Development-only identity initialization and impersonation eligibility, OpenAPI environment/contract behavior, a complete real-cookie and antiforgery API lifecycle, endpoint security metadata, reference-data administration, and the MySQL reporting/transaction/concurrency workflow.
- Request classification is advisory only: requesters can obtain and review a suggested category, priority, and concise summary before explicitly applying category and priority to the submission form. The form warns that title and description text may be sent to a configured external AI service and tells users not to enter sensitive information. Inputs use the same 200-character title and 4,000-character description limits as request creation, provider storage is disabled, and classification calls are limited to ten per authenticated user per minute. Local deterministic rules are always available. Set `RequestClassification__OpenAi__ApiKey` (and optionally `RequestClassification__OpenAi__Model`) only as an environment variable or user secret to enable the optional OpenAI-backed adviser; invalid or unavailable responses fall back to the local rules. No API key belongs in configuration files or source control.

## License

OperationsHub is available under the [MIT License](LICENSE).
