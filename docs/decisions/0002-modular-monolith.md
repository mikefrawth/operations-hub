# ADR 0002: Modular monolith

- Status: Accepted
- Date: 2026-07-28

## Context

Request assignment, status changes, comments, and auditing share transactional and reporting needs. The project needs defensible boundaries but not independent deployment.

## Decision

Build one deployable application with separate Domain, Application, Infrastructure, and Web projects and enforce one-way dependencies.

## Consequences

Cross-feature transactions and local development remain straightforward while project boundaries improve maintainability and testing. Modules cannot deploy independently. Future extraction requires evidence such as independent ownership, scaling, or release cadence—not architectural fashion.
