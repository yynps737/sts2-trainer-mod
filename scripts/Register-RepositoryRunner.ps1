param(
    [Parameter(Mandatory = $false)]
    [string]$Owner = 'yynps737',

    [Parameter(Mandatory = $false)]
    [string]$Repository = 'sts2-trainer-mod',

    [Parameter(Mandatory = $false)]
    [string]$InstallRoot = "$env:USERPROFILE\actions-runner\sts2-trainer-mod",

    [Parameter(Mandatory = $false)]
    [string]$RunnerName = "$env:COMPUTERNAME-sts2-build",

    [Parameter(Mandatory = $false)]
    [string]$Labels = 'sts2-build',

    [Parameter(Mandatory = $false)]
    [switch]$SkipServiceInstall
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Assert-Admin {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        throw 'Administrator privileges are required to install the runner as a Windows service.'
    }
}

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    throw 'GitHub CLI not found.'
}

if (-not $SkipServiceInstall) {
    Assert-Admin
}

$repoRef = "$Owner/$Repository"
$downloads = gh api "repos/$repoRef/actions/runners/downloads" | ConvertFrom-Json
$asset = $downloads | Where-Object { $_.os -eq 'win' -and $_.architecture -eq 'x64' } | Select-Object -First 1
if (-not $asset) {
    throw 'Could not resolve a Windows x64 runner package.'
}

$tokenResponse = gh api -X POST "repos/$repoRef/actions/runners/registration-token" | ConvertFrom-Json
if (-not $tokenResponse.token) {
    throw 'Failed to obtain a runner registration token.'
}

New-Item -ItemType Directory -Path $InstallRoot -Force | Out-Null
$archivePath = Join-Path $InstallRoot $asset.filename
Invoke-WebRequest -Uri $asset.download_url -OutFile $archivePath

$archiveHash = (Get-FileHash -Algorithm SHA256 $archivePath).Hash.ToLowerInvariant()
if ($archiveHash -ne $asset.sha256_checksum.ToLowerInvariant()) {
    throw 'Runner archive checksum mismatch.'
}

Get-ChildItem -Path $InstallRoot -Force |
    Where-Object { $_.Name -ne $asset.filename } |
    Remove-Item -Recurse -Force

Expand-Archive -Path $archivePath -DestinationPath $InstallRoot -Force

$configArguments = @(
    '--unattended',
    '--url', "https://github.com/$repoRef",
    '--token', $tokenResponse.token,
    '--name', $RunnerName,
    '--labels', $Labels,
    '--replace'
)

if (-not $SkipServiceInstall) {
    $configArguments += '--runasservice'
}

Push-Location $InstallRoot
try {
    & .\config.cmd @configArguments
    if ($LASTEXITCODE -ne 0) {
        throw "Runner configuration failed with exit code $LASTEXITCODE."
    }
}
finally {
    Pop-Location
}

Write-Host "Runner configured for $repoRef"
Write-Host "Install root: $InstallRoot"
Write-Host "Runner name:  $RunnerName"
Write-Host "Labels:       $Labels"
