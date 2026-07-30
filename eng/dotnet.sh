#!/usr/bin/env bash

set -euo pipefail

script_directory="${BASH_SOURCE[0]%/*}"

if [[ "${script_directory}" == "${BASH_SOURCE[0]}" ]]; then
    script_directory="."
fi

repository_root="$(cd "${script_directory}/.." && pwd)"
export DOTNET_CLI_HOME="${DOTNET_CLI_HOME:-${repository_root}/.dotnet-cli-home}"
export DOTNET_CLI_TELEMETRY_OPTOUT="${DOTNET_CLI_TELEMETRY_OPTOUT:-1}"
export NUGET_PACKAGES="${NUGET_PACKAGES:-${repository_root}/.nuget/packages}"

dotnet_arguments=("$@")
dotnet_command="dotnet"
repository_dotnet="${repository_root}/.dotnet/dotnet"

case "${OSTYPE:-}" in
    cygwin* | msys* | win32*)
        repository_dotnet="${repository_root}/.dotnet/dotnet.exe"
        ;;
esac

if [[ -x "${repository_dotnet}" ]]; then
    dotnet_command="${repository_dotnet}"
fi

if [[ $# -gt 0 ]]; then
    add_nuget_config=false

    if [[ "$1" == "restore" ]]; then
        add_nuget_config=true
    elif [[ $# -gt 1 && "$1" == "tool" && "$2" == "restore" ]]; then
        add_nuget_config=true
    fi

    if [[ "${add_nuget_config}" == "true" ]]; then
        has_config_file=false

        for argument in "${dotnet_arguments[@]}"; do
            if [[ "${argument}" == "--configfile" ]]; then
                has_config_file=true
                break
            fi
        done

        if [[ "${has_config_file}" == "false" ]]; then
            dotnet_arguments+=(--configfile "${repository_root}/NuGet.Config")
        fi
    fi
fi

if [[ -n "${CODEX_CI:-}${CODEX_THREAD_ID:-}" && $# -gt 0 ]]; then
    case "$1" in
        build | restore | test)
            # Codex's sandbox does not support MSBuild's default worker/server IPC reliably.
            export DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER=1
            dotnet_arguments+=(--disable-build-servers -m:1)
            ;;
    esac
fi

exec "${dotnet_command}" "${dotnet_arguments[@]}"
