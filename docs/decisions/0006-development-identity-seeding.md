# Development identity seeding

Status: accepted

## Context

The initial migration embedded fixed demo users and public password hashes with schema seed data. EF applies model seed data in every environment, so a migration-only non-development deployment could receive usable demo accounts. Deleting those user rows could also violate the project's requirement to preserve historical request relationships.

## Decision

The EF model seeds stable roles and reference data but no users, role assignments, or password hashes. A remediation migration removes demo-role assignments, clears the historical password hashes, changes security stamps, and locks the fixed demo identities without deleting their rows. The migration does not restore public credentials when rolled back.

An environment-guarded Development initializer applies migrations and then creates or restores the local demo users through ASP.NET Core Identity. It enables failed-attempt lockout and assigns the intended demo roles. Non-development configuration must provide its own database connection and identity provisioning.

## Consequences

- Migration-only and non-development deployments do not retain usable public demo credentials.
- Existing foreign-key history that references a fixed demo user remains valid.
- Local Development startup remains convenient and idempotent.
- Running only `dotnet ef database update` no longer creates usable demo logins; the Development web host must start to initialize them.
- A future production identity-provisioning workflow remains required.
