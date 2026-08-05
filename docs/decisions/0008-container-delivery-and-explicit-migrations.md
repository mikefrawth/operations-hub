# Container delivery and explicit migrations

## Context

Portfolio reviewers need a low-friction local demonstration with one portable artifact, health checks, and controlled schema changes. Running the web project directly alongside containerized MySQL requires the .NET SDK and produces a different runtime path from the supported full-stack Compose demonstration. The original decision also considered a future public deployment; that portion is **Archived / not currently planned** by [decision 0011](0011-local-portfolio-demonstration.md).

## Decision

Build `OperationsHub.Web` as a pinned, multi-stage Linux container image that runs as a non-root user. Docker Compose runs that image with MySQL for an isolated Development demonstration and persists both database data and Data Protection keys in named volumes. The same image accepts `--migrate` as a one-shot mode that applies EF Core migrations and exits without starting HTTP or initializing Development demo identities.

The image performs its publish-time restore evaluation after application source is copied. This retains the project-file restore cache while ensuring .NET 10 discovers and publishes the Blazor static-web-assets runtime. The image health check verifies that runtime script in addition to the process-only health endpoint. The private Development Compose connection disables TLS and permits MySQL public-key retrieval so a fresh MySQL 8.4 volume can authenticate; it must not be copied into a persistent public environment.

Expose separate process-liveness and database-readiness endpoints. Keep credentials and environment-specific configuration outside the image. The explicit forwarded-header setting remains dormant architecture for a deliberately trusted ingress if the archived hosted option is reconsidered.

## Consequences

A reviewer can start the complete local application with one Compose command and does not need a local .NET SDK or MySQL installation. Build, migration, and runtime use the same image. The Compose configuration deliberately enables Development demo data and known credentials, so remote access is limited to a controlled, scheduled Cloudflare Quick Tunnel session that is stopped immediately afterward.

The former managed-MySQL, protected shared-key storage, trusted-ingress, release, and rollback requirements are retained only in the archived hosted-production section of [deployment.md](../deployment.md). They are not current implementation requirements.

## Status

Accepted for local container delivery and explicit migrations. The hosted-deployment portion is **Archived / not currently planned** by decision 0011.
