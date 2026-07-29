# Transactional assignment procedure

## Context

The request assignment workflow changes current request state and appends both assignment and audit history. Milestone 4 needs a concrete MySQL transaction and parameterized SQL demonstration without moving ordinary CRUD out of EF Core.

## Decision

Use `sp_assign_request` for manager and administrator assignments. The procedure locks the target request with `FOR UPDATE`, checks the client-supplied version, updates the request and version, appends both history records, and commits. Missing, closed, and stale requests return explicit outcomes; the stale path rolls back before any write. Infrastructure invokes it with a parameterized `DbCommand` and translates its outcome into Application results.

## Consequences

Assignment has one MySQL transaction boundary and demonstrable rollback behavior. EF Core remains responsible for normal request creation, updates, status changes, and reads. Every assignment client must round-trip the request version, and a `409 Conflict` response requires a refresh before retrying.
