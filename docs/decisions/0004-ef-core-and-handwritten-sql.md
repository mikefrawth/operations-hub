# ADR 0004: EF Core plus selected handwritten SQL

- Status: Accepted
- Date: 2026-07-28

## Context

Routine application persistence benefits from change tracking, mappings, and migrations, while the portfolio also needs credible SQL, transaction, and reporting work.

## Decision

Use EF Core for normal persistence and migrations. Use parameterized handwritten SQL for selected reporting and transactional operations where it is clearer or demonstrates a meaningful MySQL capability.

## Consequences

Most code remains maintainable and provider integration stays explicit. SQL artifacts require synchronization with migrations, MySQL integration tests, and careful mapping. Not all CRUD will be forced through stored procedures.
