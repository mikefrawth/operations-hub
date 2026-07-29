# Database artifacts guide

This directory owns reviewed SQL and database documentation artifacts that complement EF Core migrations.

- Put migration-related standalone SQL in `migrations`, procedures in `procedures`, views in `views`, development seed scripts in `seed`, and diagrams in `diagrams`.
- Use lowercase snake_case for physical MySQL objects and stable `vw_`/`sp_` prefixes for views/procedures.
- Qualify columns in joins, list columns explicitly, and parameterize all application-supplied values.
- Add comments for non-obvious locking, transaction, performance, or MySQL-specific behavior.
- Design foreign keys and delete behavior to preserve operational and audit history.
- Keep scripts repeatable where practical and never include production data or credentials.
- Update `docs/database.md`, the ER diagram, and database integration tests with schema changes.
- Validate execution against MySQL 8.x and document `EXPLAIN` results for performance-sensitive queries.

Do not make EF migrations and standalone schema scripts competing sources of truth. EF migrations own application schema evolution; standalone files demonstrate and document selected database features.
