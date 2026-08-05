# 0011: Local portfolio demonstration and archived live hosting

- Status: Accepted
- Date: 2026-08-03

## Context

OperationsHub is a portfolio project whose complete application already runs locally through Docker Compose. Keeping Azure resources or another continuously hosted production environment active would add ongoing cloud costs without being necessary to demonstrate the application, database, container, security, and testing work.

Earlier planning considered Azure resource provisioning, Azure Container Registry, Container Apps, Azure Database for MySQL, private VNet networking, Azure Files for Data Protection keys, GitHub OIDC deployment, hosted smoke tests, and production rollback. Provider-neutral documentation also described the same live-hosting concerns as pending work.

## Decision

Azure and all other live-production hosting work is **Archived / not currently planned**. It is not a prerequisite, next milestone, required deployment step, exit gate, or definition-of-done item. The architecture notes may be retained as a clearly labeled future option, but no documentation may imply that cloud resources are active or pending, and no workflow may require Azure credentials, ACR, or a live environment.

The supported portfolio demonstration runs the complete application and MySQL locally with Docker Compose. For a scheduled remote demonstration, a Cloudflare Quick Tunnel may expose only the local web port through a temporary random `trycloudflare.com` HTTPS URL. The URL is shared only with the intended attendees, the laptop remains awake with Docker Compose and the tunnel process running, and the tunnel is stopped immediately after the session. A short recorded walkthrough is kept as a backup.

Development-only accounts and sensitive configuration remain limited to the controlled, time-limited demonstration. The tunnel URL is not published, MySQL is not tunneled, and the temporary tunnel is not represented as a production deployment.

## Consequences

- Ongoing Azure hosting costs are avoided.
- Local Docker Compose remains the single supported full-application topology.
- Temporary remote demos have no uptime guarantee and receive a new random URL whenever the tunnel restarts.
- The demo depends on the laptop, Docker Compose, and `cloudflared` remaining online for the entire session.
- CI may verify and publish a container build artifact, but it does not deploy that artifact to a live environment.
- Reactivating hosted production would require a new decision, budget approval, implementation, security review, and updated documentation.
