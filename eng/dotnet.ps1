Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$dotnetArguments = @($args)

if (-not $env:DOTNET_CLI_HOME) {
    $env:DOTNET_CLI_HOME = Join-Path $repositoryRoot '.dotnet-cli-home'
}

if (-not $env:DOTNET_CLI_TELEMETRY_OPTOUT) {
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
}

if (-not $env:NUGET_PACKAGES) {
    $env:NUGET_PACKAGES = Join-Path $repositoryRoot '.nuget\packages'
}

# Codex can supply both Path and PATH. Start-Process treats them as duplicate
# keys on Windows, so collapse them before dotnet or its child processes start.
$processEnvironment = [Environment]::GetEnvironmentVariables()
$pathKeys = @(
    $processEnvironment.Keys |
        Where-Object {
            [string]::Equals(
                [string] $_,
                'Path',
                [StringComparison]::OrdinalIgnoreCase
            )
        }
)

if ($pathKeys.Count -gt 1) {
    $pathValue = [string] $processEnvironment[$pathKeys[0]]

    foreach ($pathKey in $pathKeys) {
        [Environment]::SetEnvironmentVariable(
            [string] $pathKey,
            $null,
            [EnvironmentVariableTarget]::Process
        )
    }

    [Environment]::SetEnvironmentVariable(
        'Path',
        $pathValue,
        [EnvironmentVariableTarget]::Process
    )
}

$repositoryDotnet = Join-Path $repositoryRoot '.dotnet\dotnet.exe'
$dotnetCommand = if (Test-Path -LiteralPath $repositoryDotnet) {
    $repositoryDotnet
}
else {
    (Get-Command dotnet.exe -ErrorAction Stop).Source
}

$effectiveArguments = @($DotnetArguments)
$isRestore = $effectiveArguments.Count -gt 0 -and $effectiveArguments[0] -eq 'restore'
$isToolRestore =
    $effectiveArguments.Count -gt 1 -and
    $effectiveArguments[0] -eq 'tool' -and
    $effectiveArguments[1] -eq 'restore'
$hasConfigFile = $effectiveArguments -contains '--configfile'

if (($isRestore -or $isToolRestore) -and -not $hasConfigFile) {
    $effectiveArguments += @(
        '--configfile',
        (Join-Path $repositoryRoot 'NuGet.Config')
    )
}

$isCodexEnvironment = $env:CODEX_CI -or $env:CODEX_THREAD_ID

if ($isCodexEnvironment -and $effectiveArguments.Count -gt 0) {
    if ($effectiveArguments[0] -in @('build', 'restore', 'test')) {
        $env:DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER = '1'
        $effectiveArguments += @('--disable-build-servers', '-m:1')
    }
}

& $dotnetCommand @effectiveArguments
exit $LASTEXITCODE
