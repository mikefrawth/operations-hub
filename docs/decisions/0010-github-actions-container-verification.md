# 0010: GitHub Actions container verification and artifact publication

## Context

The repository has a reproducible Docker image, a local Compose demonstration, and a migration-only image mode, but it previously relied on a developer to run every delivery check. The source repository is on GitHub and needs repeatable pull-request verification plus a source-controlled way to publish the exact image that passed those checks.

## Decision

GitHub Actions runs the .NET restore, build, test, and formatting gates for every pull request and push to `main`. A dependent job builds the full Compose stack, waits for database-backed readiness, checks the liveness endpoint, landing page, Blazor framework asset, and runs the image's `--migrate` mode. Only a successful push to `main` publishes the built image to GitHub Container Registry with both `sha-<commit>` and `latest` tags.

Image publication is artifact delivery only, not a deployment. No workflow provisions or updates a hosted environment, and no workflow may require Azure credentials, ACR, GitHub OIDC deployment, or a live environment while [decision 0011](0011-local-portfolio-demonstration.md) remains active.

## Consequences

Pull requests receive the same quality and container checks without needing registry write access. A successful `main` build creates a traceable build artifact. The mutable `latest` tag is convenient for local evaluation, while the SHA tag identifies the exact verified source revision. No hosted environment currently consumes either tag.

## Status

Accepted for CI verification and artifact publication. Hosted release automation is **Archived / not currently planned** by decision 0011.
