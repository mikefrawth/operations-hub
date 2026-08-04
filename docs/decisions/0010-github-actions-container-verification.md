# 0010: GitHub Actions container verification and publication

## Context

The repository has a reproducible Docker image, a local Compose demonstration, and a migration-only image mode, but it previously relied on a developer to run every delivery check. The project is hosted on GitHub and needs repeatable pull-request verification plus a source-controlled way to publish the exact image that passed those checks.

## Decision

GitHub Actions runs the .NET restore, build, test, and formatting gates for every pull request and push to `main`. A dependent job builds the full Compose stack, waits for database-backed readiness, checks the liveness endpoint, landing page, Blazor framework asset, and runs the image's `--migrate` mode. Only a successful push to `main` publishes the Dockerfile to GitHub Container Registry with both `sha-<commit>` and `latest` tags.

Deployment is not automated until a hosting provider and its secret-injection model are selected. A release must deploy the immutable SHA tag, run the documented migration task, smoke-test readiness, and retain the prior SHA tag for rollback.

## Consequences

Pull requests receive the same quality and container checks without needing registry write access. A successful `main` build creates a traceable image suitable for a provider-specific release workflow. The mutable `latest` tag is convenient for local evaluation but is not a deployment target; releases use the immutable SHA tag. GHCR package visibility and any hosted deployment credentials require repository-owner configuration outside the repository.

## Status

Accepted
