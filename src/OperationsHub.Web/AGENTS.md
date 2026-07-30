# Web layer guide

## Responsibility

This project is the ASP.NET Core host, Blazor UI, REST API surface, middleware pipeline, and dependency-injection composition root.

## Dependencies and placement

- It may reference Application and Infrastructure.
- Do not add a direct Domain reference; reach domain behavior through Application use cases.
- Put Razor pages/components under `Components`, API endpoints/controllers under a clearly named API area, and composition extensions near `Program.cs`.
- Keep components and endpoints thin. They translate input/output and delegate authorization-aware work to Application.
- Do not reference EF Core types, DbContext, or persistence entities from Razor components or API DTOs.

## Conventions and testing

- Name pages for user tasks and give every route an accessible title and heading.
- Provide labels, validation summaries, loading/empty states, and safe error feedback.
- Enforce authorization server-side even when navigation or controls are hidden.
- Require antiforgery validation on every cookie-authenticated mutation endpoint and rate-limit credential endpoints.
- Use Problem Details for API errors and retain trace identifiers in safe error responses.
- Add endpoint and representative authentication/authorization coverage to IntegrationTests.
- Run `.\eng\dotnet.cmd run --project src/OperationsHub.Web` for local UI checks in native Windows/PowerShell environments.

Avoid template/demo pages, business logic in `.razor` files, unbounded lists, stack-trace disclosure, and service location from components.
