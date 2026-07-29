# Source projects guide

This directory contains production code. Preserve the dependency direction `Web -> Infrastructure -> Application -> Domain`; Web also references Application for use-case contracts, and Infrastructure may reference Domain directly for persistence mapping.

- Match namespaces to project and folder names.
- Keep each type focused and prefer feature-oriented subfolders within each layer.
- Do not share code by adding reverse or cyclic project references.
- Do not add cross-cutting abstractions until at least two concrete consumers demonstrate the need.
- Run `./eng/dotnet.sh build --no-restore` after source changes and the relevant test projects before declaring completion.

Read the project-specific `AGENTS.md` before editing a project.
