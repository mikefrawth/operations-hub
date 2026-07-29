# ADR 0005: Soft deactivation for reference data

- Status: Accepted
- Date: 2026-07-28

## Context

Departments and request types may be retired after operational requests reference them. Physical deletion would damage historical meaning or require unsafe cascades.

## Decision

Deactivate departments and request types instead of deleting records through normal application workflows once they may be referenced.

## Consequences

Historical requests retain accurate relationships and administrators can control future selection. Queries and validation must consistently distinguish active choices from historical records, and uniqueness rules must define whether names can be reused after deactivation.
