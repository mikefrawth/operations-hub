# API and OpenAPI

## Document availability

OperationsHub uses the first-party `Microsoft.AspNetCore.OpenApi` generator owned by the Web composition root. The OpenAPI JSON is available at `GET /openapi/v1.json` only when the host runs in `Development`. The supported Docker Compose demo uses `Development`, so the document is available at `http://localhost:5090/openapi/v1.json` after the stack is healthy.

The route is not mapped in Production or other non-Development environments. No Swagger UI, Scalar UI, or other interactive API console is installed. The document describes the authenticated `/api` surface; page-form endpoints, administrator impersonation, health probes, static assets, and Blazor routes are intentionally excluded.

## Authentication and antiforgery

The REST API is a same-origin companion to the Blazor application, not a standalone bearer-token API. It uses the ASP.NET Core Identity application cookie obtained through the antiforgery-protected `POST /sign-in` form. The OpenAPI document records that cookie scheme, but it cannot perform the HTML sign-in flow for a client.

For a non-browser client:

1. `GET /sign-in`, retain the antiforgery cookie, and extract the form's `__RequestVerificationToken` value.
2. `POST /sign-in` as `application/x-www-form-urlencoded` with email, password, return URL, and that form token; retain the Identity cookie from the response.
3. `GET /api/antiforgery` with the Identity cookie. Retain the antiforgery cookie and read `requestToken` plus `headerName` from the JSON response.
4. For every JSON `POST` or `PUT`, send both cookies and the returned token in the returned header. The configured header is `RequestVerificationToken`.

Missing or invalid antiforgery state returns `400` before application code executes. Missing authentication returns `401`; a signed-in actor outside the required role or data scope receives `403`. OperationsHub does not currently expose a bearer-token flow or a cross-origin CORS contract, so generated clients still need explicit cookie and antiforgery handling.

## Authorization surface

| Area | Permitted actors |
| --- | --- |
| Search or retrieve requests | Requesters for owned requests; Technicians for assigned requests; Managers and Administrators for all requests |
| Create or update a request | Requester; updates are limited to the request owner and permitted fields |
| Request classification suggestion | Requester; advisory only and rate-limited |
| Assign an active technician | Manager or Administrator |
| Change status | Manager, Administrator, or the assigned Technician |
| Add a comment | Any actor who may view the request |
| Open-request and department-performance reports | Manager or Administrator |
| Department and request-type administration | Administrator |

The endpoint policy is the first server boundary; Application services repeat authorization-aware workflow decisions using mapped actors. UI visibility is not treated as authorization.

## Status and concurrency semantics

- Successful reads and ordinary mutations return `200`; request creation returns `201` with a `Location` header.
- Validation and antiforgery failures return RFC 7807-compatible `400` responses.
- Missing records return `404` without exposing persistence details.
- A stale request `version` returns `409`; clients must retrieve current state before retrying.
- Classification throttling returns `429`.
- Unhandled API failures are converted to safe `500` Problem Details; stack traces and configuration are not returned.

Operation summaries, request/response schemas, relevant status codes, role descriptions, the cookie scheme, and required mutation header are emitted in the OpenAPI document. Runtime authorization, antiforgery validation, optimistic concurrency, and persistence behavior remain authoritative.

## Verification

`OpenApiDocumentTests` retrieves and inspects the generated Development document, verifies the documented cookie/antiforgery contract, and proves both the JSON route and a Swagger-style UI are absent in Production. `ServiceRequestApiLifecycleTests` uses real Requester and Manager cookie sessions plus antiforgery state to create, retrieve, update, assign, progress, comment on, and finally verify a request through HTTP and real MySQL persistence.
