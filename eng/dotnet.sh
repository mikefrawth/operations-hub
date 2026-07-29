#!/usr/bin/env bash

set -euo pipefail

repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
local_dotnet="${repository_root}/.dotnet/dotnet"

export DOTNET_CLI_HOME="${DOTNET_CLI_HOME:-${repository_root}/.dotnet-cli-home}"
export DOTNET_CLI_TELEMETRY_OPTOUT="${DOTNET_CLI_TELEMETRY_OPTOUT:-1}"
export NUGET_PACKAGES="${NUGET_PACKAGES:-${repository_root}/.nuget/packages}"

if [[ -x "${local_dotnet}" ]]; then
    export DOTNET_ROOT="${repository_root}/.dotnet"
    if [[ -d "${repository_root}/.dotnet/native" ]]; then
        export LD_LIBRARY_PATH="${repository_root}/.dotnet/native${LD_LIBRARY_PATH:+:${LD_LIBRARY_PATH}}"
    fi

    exec "${local_dotnet}" "$@"
fi

exec dotnet "$@"
