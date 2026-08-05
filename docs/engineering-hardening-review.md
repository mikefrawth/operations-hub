# Milestone 5 engineering-hardening review

Milestone 5 closed on 2026-08-05 after implementation review, real-MySQL testing, and browser verification of the Development portfolio scenario. This review records what was checked and avoids implying certifications or load results that were not performed.

## Security

- Administrative and workflow authorization remains enforced by server-side policies and authorization-aware application services.
- Cookie-authenticated mutation endpoints carry antiforgery requirements; sign-in is both antiforgery-protected and rate-limited.
- Identity lockout, HTTP-only same-site cookies, safe local return URLs, defensive response headers, centralized exception handling, and non-disclosing API Problem Details remain in place.
- Development identities, impersonation, and the portfolio scenario remain behind the Development environment guard. Migration-only mode does not initialize them.
- No secrets, production credentials, or hosted-environment dependencies were introduced.

## Accessibility and frontend behavior

- Every page retains a title and one primary heading, and the shared layout retains its skip link and keyboard focus target.
- Forms expose visible labels, safe validation feedback, and loading, empty, success, or error states. Request creation now reports all required fields through a validation summary and field messages.
- Repeated reference-data and impersonation actions have record-specific accessible names. Data tables have captions, and persisted enum values such as `InProgress` render as readable labels.
- Request search, status and priority filters, result counts, bounded pages, previous/next controls, and reset behavior are available in the Blazor frontend.
- The interface was checked at the browser's desktop viewport. This is a manual accessibility review, not a WCAG certification or an automated assistive-technology audit.

## Performance and data access

- Request searches remain server-side, no-tracking, ordered, and paged. The application clamps page size to 100; the frontend requests 10 rows per page.
- Existing indexes support request role filters, status and recency queries, request numbers, and chronological history access.
- Participant names are loaded in one bounded, no-tracking query per detail or summary load. The directory accepts at most 100 distinct IDs and returns only identifiers and display names.
- The reporting summary continues to use the stable MySQL view and parameterized database command.
- No synthetic load test or production capacity claim was made.

## Test and browser evidence

- The solution contains 16 deterministic unit tests and 14 integration tests, all passing against the current build.
- The new real-MySQL portfolio-scenario test verifies role-scoped search visibility for Requester, Technician, Manager, and Administrator; assigned/open reporting; participant display lookup; comments; status and assignment history; and audit events.
- Browser checks verified administrator reference-data actions, request search and filters, requester validation, the technician's populated assigned-request view, the manager reporting view, contextual action names, captions, and participant display names.
- Screenshots in `docs/images` were captured from the verified Development application through administrator impersonation.

## Remaining limitations

- Automated browser accessibility scanning, screen-reader testing, performance load testing, and a repository coverage threshold are not configured.
- Participant lookup falls back to the retained identifier if a historical user row cannot be resolved.
- Live-production identity, multifactor authentication, and hosted deployment remain outside the active portfolio scope; live hosting is Archived / not currently planned.
