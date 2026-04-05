param(
    [Parameter(Mandatory = $false)]
    [string]$Sts2Path,

    [Parameter(Mandatory = $false)]
    [switch]$RequireGitHubCli
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Assert-Command {
    param(
        [string]$Name
    )

    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Required command not found: $Name"
    }
}

Assert-Command -Name 'dotnet'

$candidateRoots = @()
if (-not [string]::IsNullOrWhiteSpace($Sts2Path)) {
    $candidateRoots += $Sts2Path
}

if (-not [string]::IsNullOrWhiteSpace($env:STS2_PATH)) {
    $candidateRoots += $env:STS2_PATH
}

$candidateRoots += @(
    'D:\SteamLibrary\steamapps\common\Slay the Spire 2',
    'C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2'
)

$resolvedGameRoot = $candidateRoots |
    Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
    Select-Object -Unique |
    Where-Object { Test-Path $_ } |
    Select-Object -First 1

if (-not $resolvedGameRoot) {
    throw 'Slay the Spire 2 install not found. Set STS2_PATH or provide -Sts2Path.'
}

$dataDir = Join-Path $resolvedGameRoot 'data_sts2_windows_x86_64'
if (-not (Test-Path $dataDir)) {
    throw "STS2 data directory not found: $dataDir"
}

if ($RequireGitHubCli) {
    Assert-Command -Name 'gh'
}

Write-Host "Release environment verified."
Write-Host "Game root: $resolvedGameRoot"
Write-Host "Data dir:  $dataDir"
if ($RequireGitHubCli) {
    Write-Host 'GitHub CLI: available'
}
