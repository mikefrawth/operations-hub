#!/usr/bin/env bash

set -euo pipefail

repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
export DOTNET_CLI_HOME="${DOTNET_CLI_HOME:-${repository_root}/.dotnet-cli-home}"
export DOTNET_CLI_TELEMETRY_OPTOUT="${DOTNET_CLI_TELEMETRY_OPTOUT:-1}"
export NUGET_PACKAGES="${NUGET_PACKAGES:-${repository_root}/.nuget/packages}"

dotnet_arguments=("$@")

if [[ -n "${CODEX_CI:-}" && $# -gt 0 ]]; then
    case "$1" in
        build | restore | test)
            # Codex's sandbox does not support MSBuild's default worker/server IPC reliably.
            export DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER=1
            dotnet_arguments+=(--disable-build-servers -m:1)
            ;;
    esac
fi

exec dotnet "${dotnet_arguments[@]}"
