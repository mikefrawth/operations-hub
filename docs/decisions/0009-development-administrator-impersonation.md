# Development administrator impersonation

## Context

The four role-specific workflows are easier to verify when a developer can move between profiles without repeatedly signing out and entering known demo credentials. A general production impersonation feature would create a privileged support-access path that needs additional governance, monitoring, and operational controls beyond the current portfolio scope.

## Decision

Provide administrator impersonation only when the host runs in the Development environment. An administrator can select an active user with exactly one Requester, Technician, or Manager role. Administrator accounts, locked accounts, and accounts with multiple roles are excluded.

Starting impersonation creates a new Identity principal for the target user, removes its generated role claims, and adds only the role returned by the authorization-aware Application service. Signed and encrypted authentication-cookie claims retain the original administrator identifier, display name, target display name, and start time. The original administrator role is never copied into the target session.

The shared layout displays a prominent test-session banner on every page. Its antiforgery-protected return action revalidates that the original account is still an active administrator before issuing a fresh administrator principal. Both session transitions use authenticated POST endpoints with antiforgery validation. Start and end events are written to `audit_events` with the administrator as actor, the target user identifier, request trace identifier, remote address, and timestamp.

## Consequences

- Local role testing needs only one administrator login.
- Authorization and business actions run as the target user; administrator privileges do not leak into the test session.
- Normal request audit records identify the effective target user, while the paired impersonation events preserve who initiated the session.
- Signing out during an impersonated session clears the effective target session and never restores the original administrator.
- A demoted, locked, or deleted original administrator cannot use the return action and must sign out.
- The endpoints and navigation are absent outside Development, and the page also requires a policy that can succeed only in Development.
- Production support impersonation remains intentionally unimplemented and would require a separate decision.

## Status

Accepted.
