# Infrastructure layer guide

## Responsibility

This project implements database persistence, ASP.NET Core Identity persistence, EF Core mappings and migrations, MySQL-specific queries/procedures, transactions, and external adapters.

## Dependencies and placement

- It may reference Application and Domain.
- It must not reference Web.
- Keep DbContext and mappings under a persistence area and external implementations grouped by capability.
- Implement Application contracts without leaking provider-specific types across the boundary.
- Use EF Core for ordinary persistence and parameterized handwritten SQL for justified reporting or transactional cases.

## Database and testing rules

- Configure table/column lengths, indexes, unique constraints, foreign keys, precision, concurrency, and delete behavior explicitly.
- Avoid cascades that could erase request or audit history.
- Keep migrations deterministic and review generated SQL.
- Keep users, password hashes, and reusable credentials out of EF model seed data. Development demo identities must use the environment-guarded runtime initializer.
- Put durable standalone SQL artifacts in `/database`; keep application invocation code here.
- Use explicit transactions for multi-write business operations and document surprising isolation/locking choices.
- Test relational mappings, constraints, transactions, procedures, and raw SQL against disposable MySQL—not EF Core InMemory.

Avoid generic-repository wrappers over DbContext, concatenated SQL, provider details in Application, catch-and-ignore behavior, and migrations edited without matching model changes.
