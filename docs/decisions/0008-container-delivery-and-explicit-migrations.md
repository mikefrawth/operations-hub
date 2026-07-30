# Container delivery and explicit migrations

## Context

Portfolio reviewers need a low-friction local demonstration, while a future public deployment needs one portable artifact, environment-based configuration, health probes, and controlled schema changes. Running the web project directly alongside containerized MySQL requires the .NET SDK and produces a different runtime path from hosted deployment. Automatically applying migrations from every production web replica would create startup races and mix release operations with request serving.

## Decision

Build `OperationsHub.Web` as a pinned, multi-stage Linux container image that runs as a non-root user. Docker Compose runs that image with MySQL for an isolated Development demonstration and persists both database data and Data Protection keys in named volumes. The same image accepts `--migrate` as a one-shot mode that applies EF Core migrations and exits without starting HTTP or initializing Development demo identities.

Expose separate process-liveness and database-readiness endpoints. Support forwarded headers only through an explicit setting for deployments whose trusted ingress is the container's sole network path. Keep production credentials and provider configuration outside the image.

## Consequences

A reviewer can start the complete local application with one Compose command and does not need a local .NET SDK or MySQL installation. Build, migration, and runtime use the same immutable artifact. Production release automation must run the migration task before switching traffic, provide a TLS-enabled managed-MySQL connection string, persist or externalize Data Protection keys, and configure a trusted HTTPS ingress. The local Compose configuration remains unsuitable for internet exposure because it deliberately enables Development demo data and known credentials.

## Status

Accepted.
