# OperationsHub engineering guide

## Instructions

### Only the author can edit Instructions section

- When working on any milestone, make sure to update the web frontend as well.
- Request only narrowly scoped sandbox escalations with a clear justification. Never request unrestricted shell access when a command-specific permission or safe alternative is available.
- Before choosing a sequential branch number, check both local and remote branches to confirm the next available number.

## Git workflow

- Create a new branch before implementing each new feature.
- Name feature branches `xxxx-[branch-name]`, where `xxxx` is a sequential four-digit number beginning with `0001`. The Milestone 0 branch is `0001-foundation`.
- Git staging and commits are authorized when they are part of the requested work. Inspect the worktree and staged diff before committing; do not include unrelated changes.
- For local GitHub deployment instructions, including the author-specific SSH workflow, refer to `LOCAL_GITHUB_DEPLOYMENT.md`. This intentionally gitignored file must never be committed or pushed.

## Scope and purpose

OperationsHub is a portfolio-quality internal service-request and workflow-management system. Build it as a pragmatic modular monolith that demonstrates production-minded .NET, Blazor, MySQL, security, testing, and documentation without speculative abstractions.

Read this file and the nearest `AGENTS.md` that applies before editing. More-local guidance takes precedence when it does not conflict with these repository-wide rules.

## Architecture and technology

- `OperationsHub.Domain` owns business state and rules and has no project dependencies.
- `OperationsHub.Application` owns use cases, DTOs, validation, and application contracts; it depends only on Domain.
- `OperationsHub.Infrastructure` implements persistence and external concerns; it depends on Application and Domain.
- `OperationsHub.Web` is the composition root and UI/API host; it depends on Application and Infrastructure.
- Use .NET 10, ASP.NET Core, a Blazor Web App with global Interactive Server rendering, nullable reference types, and implicit usings.
- Use MySQL 8.x, Entity Framework Core, and selected parameterized handwritten SQL beginning in Milestone 1.
- Prefer explicit application services over mediator, generic-repository, event-bus, or microservice abstractions.

## Standard commands

The wrapper uses the repository-local SDK when present and otherwise delegates to `dotnet` on `PATH`.

```powershell
.\eng\dotnet.cmd restore
.\eng\dotnet.cmd build --no-restore
.\eng\dotnet.cmd test --no-build
.\eng\dotnet.cmd format --verify-no-changes --no-restore
.\eng\dotnet.cmd run --project src/OperationsHub.Web
```

Use `./eng/dotnet.sh` with the same arguments on Linux, macOS, or WSL.

Full local application:

```bash
docker compose up --build -d
docker compose ps
docker compose down
```

Database-only infrastructure for native web development and integration tests:

```bash
docker compose config
docker compose up -d mysql
docker compose ps
docker compose down
docker compose down --volumes
```

EF Core tooling and migrations begin in Milestone 1. Once configured, use:

```powershell
.\eng\dotnet.cmd ef migrations add <MigrationName> `
  --project src/OperationsHub.Infrastructure `
  --startup-project src/OperationsHub.Web `
  --output-dir Persistence/Migrations
.\eng\dotnet.cmd ef database update `
  --project src/OperationsHub.Infrastructure `
  --startup-project src/OperationsHub.Web
```

Do not claim a command is verified unless it completed successfully in the current environment.

## Code and dependency rules

- Put business invariants in Domain and orchestration/authorization-aware operations in Application.
- Keep Razor components and API endpoints thin; never place business rules in the UI.
- Keep EF Core, Identity persistence, MySQL, stored procedures, and external adapters in Infrastructure.
- Use DTOs at API boundaries; do not expose persistence entities.
- Use async I/O and pass cancellation tokens through meaningful boundaries.
- Treat compiler and analyzer warnings as errors. Do not suppress warnings globally to make a build pass.
- Add comments only for non-obvious rules, transaction/security decisions, complex SQL, surprising framework behavior, or justified workarounds.
- Add selective XML documentation to important public contracts, not boilerplate to obvious members.

## Testing

- Add unit tests for domain rules, validation, authorization decisions, and deterministic application behavior.
- Add integration tests for ASP.NET Core endpoints and real MySQL behavior.
- Never use EF Core InMemory to claim coverage of relational constraints, transactions, SQL, or MySQL behavior.
- A change is not complete until relevant tests are present and restore, build, and test succeed.

## Security

- Never commit real credentials, personal data, `.env`, user-secrets content, or production configuration.
- Never put users, password hashes, or reusable credentials in EF model seed data; local demo identities must be initialized only behind an explicit Development-environment guard.
- Enforce authorization on the server; UI visibility is not a security boundary.
- Use ASP.NET Core Identity, secure cookies, antiforgery protection where applicable, parameterized SQL, and least privilege.
- Avoid mass assignment and excessive response data; validate DTOs at server boundaries.
- Do not expose stack traces, database details, or sensitive configuration outside development.
- Preserve request, assignment, status, and audit history; avoid destructive cascades.

## Documentation

- Update the README, relevant documents, roadmap, ER diagram, and nearest `AGENTS.md` whenever behavior, commands, dependencies, or architecture changes.
- Record consequential decisions in `docs/decisions`.
- Document exact verified setup and migration commands and state limitations honestly.
- Treat Azure and all other live-production hosting as **Archived / not currently planned** to avoid ongoing portfolio-project cloud costs. Do not present it as a prerequisite, pending milestone, deployment step, or definition-of-done item.
- Keep Docker Compose as the supported full-application demonstration. A scheduled remote demo may use only a temporary Cloudflare Quick Tunnel to the local web port, with the URL shared narrowly and the tunnel stopped immediately afterward.

## Definition of done

A change is done only when intended UI/API behavior works, authorization and validation are enforced, persistence behavior is correct, relevant tests pass, the solution builds without warnings, documentation and applicable `AGENTS.md` files are accurate, secrets are absent, error cases were considered, and the implementation is explainable in a senior-engineering interview.

## Codex sandbox execution

- Use `.\eng\dotnet.cmd` for restore, build, test, format, and run commands in native Windows/PowerShell environments; it invokes the repository PowerShell wrapper with a process-scoped execution-policy bypass. Use `./eng/dotnet.sh` in Bash environments. The PowerShell wrapper normalizes duplicate `Path`/`PATH` process variables and both wrappers use repository-local CLI/package directories and `NuGet.Config`.
- In Codex (`CODEX_CI` or `CODEX_THREAD_ID` present), the wrappers disable MSBuild build servers and force one build node because the sandbox does not reliably support MSBuild's default worker/server IPC.
- Do not diagnose the resulting silent `Build FAILED` with zero errors as an SDK installation problem before retrying through the wrapper's sandbox-compatible path.
- Run `dotnet test` and `dotnet format` through the wrapper inside the sandbox first; the Windows-native wrapper and local development settings are configured for sandbox compatibility.
- Request external execution only when a command needs network access, system package changes, Docker access, or another capability that remains unavailable after using the wrapper.
