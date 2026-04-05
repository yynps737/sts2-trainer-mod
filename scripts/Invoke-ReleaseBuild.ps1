param(
    [Parameter(Mandatory = $false)]
    [string]$Version,

    [Parameter(Mandatory = $false)]
    [string]$OutputRoot = 'artifacts\release',

    [Parameter(Mandatory = $false)]
    [switch]$IncludePdb,

    [Parameter(Mandatory = $false)]
    [switch]$SkipRepositoryStateCheck,

    [Parameter(Mandatory = $false)]
    [switch]$SkipCleanWorktreeCheck
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$manifestPath = Join-Path $repoRoot 'src\Sts2Trainer.Mod\Sts2Trainer.json'
$constantsPath = Join-Path $repoRoot 'src\Sts2Trainer.Shared\TrainerConstants.cs'
$projectPath = Join-Path $repoRoot 'src\Sts2Trainer.Mod\Sts2Trainer.Mod.csproj'
$changelogPath = Join-Path $repoRoot 'CHANGELOG.md'
$resolvedOutputRoot = Join-Path $repoRoot $OutputRoot
$repositoryStateScript = Join-Path $PSScriptRoot 'Test-RepositoryState.ps1'
$environmentScript = Join-Path $PSScriptRoot 'Test-ReleaseEnvironment.ps1'

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

$manifest = Get-Content -Encoding UTF8 $manifestPath | ConvertFrom-Json
$manifestVersion = [string]$manifest.version
if ([string]::IsNullOrWhiteSpace($Version)) {
    $Version = $manifestVersion
}

if ($Version -ne $manifestVersion) {
    throw "Requested version '$Version' does not match manifest version '$manifestVersion'."
}

if ($Version -notmatch '^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-[0-9A-Za-z.-]+)?$') {
    throw "Version is not valid SemVer: $Version"
}

if (-not $SkipRepositoryStateCheck) {
    $repositoryStateArguments = @('-ExecutionPolicy', 'Bypass', '-File', $repositoryStateScript)
    if ($SkipCleanWorktreeCheck) {
        $repositoryStateArguments += '-SkipCleanWorktreeCheck'
    }

    & powershell @repositoryStateArguments
    if ($LASTEXITCODE -ne 0) {
        throw "Repository state check failed with exit code $LASTEXITCODE."
    }
}

& powershell -ExecutionPolicy Bypass -File $environmentScript
if ($LASTEXITCODE -ne 0) {
    throw "Release environment check failed with exit code $LASTEXITCODE."
}

$constants = Get-Content -Raw -Encoding UTF8 $constantsPath
$gameVersion = Get-ConstantValue -Source $constants -Name 'SupportedGameVersion'
$gameCommit = Get-ConstantValue -Source $constants -Name 'SupportedCommit'
$modName = Get-ConstantValue -Source $constants -Name 'ModName'
$changelogSection = Get-ChangelogSection -Path $changelogPath -TargetVersion $Version
$assemblyVersion = '{0}.0' -f (($Version -replace '-.*$',''))

$buildOutput = Join-Path $repoRoot 'src\Sts2Trainer.Mod\.godot\mono\temp\bin\Release'
if (Test-Path $buildOutput) {
    Remove-Item -Recurse -Force $buildOutput
}

dotnet build $projectPath -c Release -p:InstallToGame=false -p:Version=$Version -p:InformationalVersion=$Version -p:AssemblyVersion=$assemblyVersion -p:FileVersion=$assemblyVersion | Out-Host
if ($LASTEXITCODE -ne 0) {
    throw "dotnet build failed with exit code $LASTEXITCODE."
}

$dllPath = Join-Path $buildOutput 'Sts2Trainer.dll'
$jsonPath = Join-Path $repoRoot 'src\Sts2Trainer.Mod\Sts2Trainer.json'
$pdbPath = Join-Path $buildOutput 'Sts2Trainer.pdb'

foreach ($requiredPath in @($dllPath, $jsonPath)) {
    if (-not (Test-Path $requiredPath)) {
        throw "Missing build artifact: $requiredPath"
    }
}

if (Test-Path $resolvedOutputRoot) {
    Remove-Item -Recurse -Force $resolvedOutputRoot
}

$stageRoot = Join-Path $resolvedOutputRoot "Sts2Trainer-v$Version"
$packageRoot = Join-Path $stageRoot 'Sts2Trainer'
New-Item -ItemType Directory -Path $packageRoot -Force | Out-Null

Copy-Item -Force $dllPath (Join-Path $packageRoot 'Sts2Trainer.dll')
Copy-Item -Force $jsonPath (Join-Path $packageRoot 'Sts2Trainer.json')
Copy-Item -Force (Join-Path $repoRoot 'README.md') (Join-Path $stageRoot 'README.md')
Copy-Item -Force (Join-Path $repoRoot 'LICENSE.md') (Join-Path $stageRoot 'LICENSE.txt')

$assetName = "sts2-trainer-v$Version-win-steam-sts2-$gameVersion-$gameCommit.zip"
$assetPath = Join-Path $resolvedOutputRoot $assetName
Compress-Archive -Path (Join-Path $stageRoot '*') -DestinationPath $assetPath -Force

$assetsToHash = [System.Collections.Generic.List[string]]::new()
$assetsToHash.Add($assetPath)

if ($IncludePdb) {
    if (-not (Test-Path $pdbPath)) {
        throw "PDB requested but not found: $pdbPath"
    }

    $symbolsRoot = Join-Path $resolvedOutputRoot "Sts2Trainer-v$Version-symbols"
    New-Item -ItemType Directory -Path $symbolsRoot -Force | Out-Null
    Copy-Item -Force $pdbPath (Join-Path $symbolsRoot 'Sts2Trainer.pdb')
    $symbolsAssetName = "sts2-trainer-v$Version-symbols.zip"
    $symbolsAssetPath = Join-Path $resolvedOutputRoot $symbolsAssetName
    Compress-Archive -Path (Join-Path $symbolsRoot '*') -DestinationPath $symbolsAssetPath -Force
    $assetsToHash.Add($symbolsAssetPath)
}

$hashLines = foreach ($asset in $assetsToHash) {
    $hash = (Get-FileHash -Algorithm SHA256 $asset).Hash.ToLowerInvariant()
    '{0}  {1}' -f $hash, (Split-Path -Leaf $asset)
}
$hashLines | Set-Content -Encoding UTF8 (Join-Path $resolvedOutputRoot 'SHA256SUMS.txt')

$releaseNotes = @"
Supported game build: $gameVersion / $gameCommit
Platform / Store: Windows Steam
Status: Verified
Default hotkey: F9

Install
- Exit the game completely.
- Extract the archive.
- Copy the bundled Sts2Trainer folder into <Slay the Spire 2>\mods\

Upgrade
- Exit the game completely.
- Replace the existing Sts2Trainer folder with the newer release contents.

Remove
- Delete <Slay the Spire 2>\mods\Sts2Trainer

Checksums
- Verify the downloaded assets against SHA256SUMS.txt before installing.

$changelogSection

Note: use the binary zip for installation. GitHub's source code archives are not installable mod packages.
"@
$releaseNotesPath = Join-Path $resolvedOutputRoot 'release-notes.md'
$releaseNotes | Set-Content -Encoding UTF8 $releaseNotesPath

Write-Host "Built $modName $Version"
Write-Host "Package:  $assetPath"
Write-Host "Checksums: $(Join-Path $resolvedOutputRoot 'SHA256SUMS.txt')"
Write-Host "Notes:    $releaseNotesPath"
