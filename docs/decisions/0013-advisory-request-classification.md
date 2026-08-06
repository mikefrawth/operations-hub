# Advisory request classification

## Context

Milestone 8 adds useful request-routing assistance, but the application must stay functional offline and must not treat a probabilistic result as a workflow decision. Request categories are administrator-managed reference data, so suggestions must remain constrained to active server-side categories.

## Decision

Expose an `IRequestClassificationService` that only requesters may call. It reads the active request-type catalog from the Application-layer reference-data contract, produces a deterministic category, priority, and concise summary, and never persists a suggestion.

The Blazor submission form displays the suggestion as advisory information. A requester must explicitly apply the suggested category and priority before the ordinary validated request-submission operation receives them. Beside the action, the UI discloses that request text may be sent to a configured external AI provider and tells requesters not to enter secrets, personal data, or other sensitive information.

An optional Infrastructure adapter can call the OpenAI Responses API when an externally supplied API key enables it. Provider-side response storage is disabled. Its structured response is validated against the active category IDs, priority enum, and non-empty summary. Missing configuration, an unsuccessful response, malformed output, or an adapter exception always leaves the deterministic suggestion in place. API keys are environment variables or user secrets and are never committed.

## Consequences

- The request workflow remains authoritative for all persisted category and priority changes.
- Local demonstrations and tests work without network access or an AI account.
- The optional provider can improve suggestions without coupling Domain or Application to an AI SDK or API credential.
- AI output is intentionally not audited or stored, and cannot add a category, bypass requester authorization, or create a request. Any future automated routing would require a separate authorization and audit decision.

## Status

Accepted (2026-08-05).
