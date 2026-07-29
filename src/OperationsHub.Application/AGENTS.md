# Application layer guide

## Responsibility

This project owns use cases, commands and queries, DTOs, validation, mapping, application contracts, authorization-aware decisions, and transaction-boundary abstractions.

## Dependencies and placement

- It may reference Domain only.
- It must not reference Web, Infrastructure, EF Core, ASP.NET Core UI types, or a MySQL provider.
- Define persistence/external-service interfaces here only when an application use case requires them.
- Organize code by capability rather than large technical buckets when features arrive.
- Use explicit services and handlers; do not add a mediator library without a documented need.

## Conventions and testing

- Name operations for intent, such as `AssignRequest` or `ChangeRequestStatus`.
- Accept DTOs or command models and return explicit results; do not leak persistence entities.
- Pass cancellation tokens to I/O-facing contracts.
- Keep transactions around complete business operations, not individual repository calls.
- Unit-test validation, orchestration, and authorization-relevant decisions with test doubles at layer boundaries.

Avoid generic repositories, duplicated domain invariants, ambient user access hidden in static helpers, and exceptions as ordinary validation flow.
