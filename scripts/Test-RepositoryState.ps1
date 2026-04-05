param(
    [Parameter(Mandatory = $false)]
    [switch]$SkipCleanWorktreeCheck
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$manifestPath = Join-Path $repoRoot 'src\Sts2Trainer.Mod\Sts2Trainer.json'
$constantsPath = Join-Path $repoRoot 'src\Sts2Trainer.Shared\TrainerConstants.cs'
$changelogPath = Join-Path $repoRoot 'CHANGELOG.md'
$releasingPath = Join-Path $repoRoot 'RELEASING.md'
$readmePath = Join-Path $repoRoot 'README.md'
$licensePath = Join-Path $repoRoot 'LICENSE.md'

function Get-ConstantValue {
    param(
        [string]$Source,
        [string]$Name
    )

    $match = [regex]::Match($Source, "public const string $Name = ""([^""]+)"";")
    if (-not $match.Success) {
        throw "Missing constant: $Name"
    }

    return $match.Groups[1].Value
}

function Get-ChangelogSection {
    param(
        [string]$Path,
        [string]$TargetVersion
    )

    $lines = Get-Content -Encoding UTF8 $Path
    $start = -1
    $end = $lines.Count

    for ($index = 0; $index -lt $lines.Count; $index++) {
        if ($lines[$index] -match ("^## \[{0}\]" -f [regex]::Escape($TargetVersion))) {
            $start = $index + 1
            continue
        }

        if ($start -ge 0 -and $lines[$index] -match '^## \[') {
            $end = $index
            break
        }
    }

    if ($start -lt 0) {
        throw "CHANGELOG section not found for version $TargetVersion"
    }

    return (($lines[$start..($end - 1)] -join [Environment]::NewLine).Trim())
}

foreach ($requiredFile in @($manifestPath, $constantsPath, $changelogPath, $releasingPath, $readmePath, $licensePath)) {
    if (-not (Test-Path $requiredFile)) {
        throw "Missing required file: $requiredFile"
    }
}

$manifest = Get-Content -Encoding UTF8 $manifestPath | ConvertFrom-Json
$constants = Get-Content -Raw -Encoding UTF8 $constantsPath
$changelog = Get-Content -Raw -Encoding UTF8 $changelogPath

$version = [string]$manifest.version
if ($version -notmatch '^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-[0-9A-Za-z.-]+)?$') {
    throw "Manifest version is not valid SemVer: $version"
}

$manifestName = [string]$manifest.name
$manifestAuthor = [string]$manifest.author
$manifestDescription = [string]$manifest.description
$modName = Get-ConstantValue -Source $constants -Name 'ModName'
$modAuthor = Get-ConstantValue -Source $constants -Name 'ModAuthor'
$modDescription = Get-ConstantValue -Source $constants -Name 'ModDescription'

if ([string]::IsNullOrWhiteSpace($manifestAuthor)) {
    throw 'Manifest author must not be empty.'
}

if ($manifestName -ne $modName) {
    throw "Manifest name '$manifestName' does not match TrainerConstants.ModName '$modName'."
}

if ($manifestAuthor -ne $modAuthor) {
    throw "Manifest author '$manifestAuthor' does not match TrainerConstants.ModAuthor '$modAuthor'."
}

if ($manifestDescription -ne $modDescription) {
    throw 'Manifest description does not match TrainerConstants.ModDescription.'
}

$supportedGameVersion = Get-ConstantValue -Source $constants -Name 'SupportedGameVersion'
$supportedCommit = Get-ConstantValue -Source $constants -Name 'SupportedCommit'

if ($supportedCommit -notmatch '^[0-9a-f]{8}$') {
    throw "SupportedCommit must be an 8-character lowercase hex string: $supportedCommit"
}

if ($changelog -notmatch '(?m)^## \[Unreleased\]$') {
    throw 'CHANGELOG.md must contain an [Unreleased] section.'
}

$currentSection = Get-ChangelogSection -Path $changelogPath -TargetVersion $version
if ($currentSection -notmatch '(?m)^### Compatibility\r?$') {
    throw "CHANGELOG entry for $version must contain a Compatibility section."
}

if ($currentSection -notmatch '(?m)^### Breaking Changes\r?$') {
    throw "CHANGELOG entry for $version must contain a Breaking Changes section."
}

if (-not $SkipCleanWorktreeCheck) {
    $statusLines = git -C $repoRoot status --porcelain
    if ($LASTEXITCODE -ne 0) {
        throw 'Failed to query git worktree status.'
    }

    if ($statusLines) {
        throw "Worktree must be clean before a release validation run.`n$($statusLines -join [Environment]::NewLine)"
    }
}

$trackedGenerated = git -C $repoRoot ls-files | Where-Object { $_ -match '(^|/)(bin|obj|\.godot)(/|$)' }
if ($LASTEXITCODE -ne 0) {
    throw 'Failed to query tracked files.'
}

if ($trackedGenerated) {
    throw "Tracked generated paths detected:`n$($trackedGenerated -join [Environment]::NewLine)"
}

Write-Host "Repository state verified."
Write-Host "Manifest version: $version"
Write-Host "Supported game build: $supportedGameVersion / $supportedCommit"
