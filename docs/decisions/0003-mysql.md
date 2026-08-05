# ADR 0003: MySQL

- Status: Accepted
- Date: 2026-07-28

## Context

OperationsHub needs explicit relational modeling, constraints, transactions, reporting queries, migrations, and a visible database-engineering component.

## Decision

Use MySQL 8.x, with MySQL 8.4 as the local Compose image. Develop and integration-test against the real engine.

## Consequences

The project can demonstrate MySQL-specific views, procedures, indexing, and query plans. Provider behavior must be tested with MySQL rather than EF Core InMemory. A managed MySQL service, cloud credential model, backups, and patching belong only to the **Archived / not currently planned** hosted-production option; local Docker Compose is the active database topology.
