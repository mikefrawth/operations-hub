# Documentation guide

Documentation must describe the implemented system and verified commands, not aspirational behavior presented as complete.

- Keep `architecture.md`, `database.md`, and `roadmap.md` synchronized with code.
- Put one consequential decision per file in `decisions` using context, decision, consequences, and status.
- Link to source or another document instead of duplicating lengthy explanations.
- Mark planned features and unverified commands clearly.
- Use Mermaid for architecture and data diagrams and keep node names aligned with project/entity names.
- Never include real credentials, personal data, environment dumps, or production-like example secrets.
- Mark Azure and any other live hosted environment **Archived / not currently planned**; preserve useful design notes only in a clearly separated archive.
- Document Docker Compose as the supported complete demo and Cloudflare Quick Tunnel only as temporary, controlled remote access with no production or uptime claim.
- Update the README’s feature status and known limitations at each milestone.

Use concise language suitable for an interviewer or a new maintainer.
