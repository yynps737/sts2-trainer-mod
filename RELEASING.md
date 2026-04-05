# Releasing

## Source Of Truth

- Release version lives in `src/Sts2Trainer.Mod/Sts2Trainer.json`.
- Supported game build lives in `src/Sts2Trainer.Shared/TrainerConstants.cs`.
- Git tags are derived from the manifest version and always use the `v` prefix.

Do not maintain a second manual mod-version constant anywhere else.

## Versioning

- Mod versions use `SemVer`.
- Stable tags use `vX.Y.Z`.
- Pre-release tags use the same version string with a SemVer suffix, for example `v0.2.0-beta.1` or `v0.2.0-rc.1`.
- The GitHub release type is inferred from the version string. Do not mark a release as prerelease with a stable tag.
- The mod version does not encode the game build.

## SemVer Rules

- `PATCH`
  - Packaging fixes
  - Crash fixes
  - Small behavior corrections
  - Documentation-only release corrections
  - No intentional breaking change in settings, install layout, or exposed feature names
- `MINOR`
  - New user-facing features
  - Non-breaking settings additions
  - Support for a newly validated game build without breaking the existing install path or settings shape
- `MAJOR`
  - Breaking settings/schema changes
  - Removed or renamed shipped features
  - Install layout changes
  - Behavior changes that require a manual migration or invalidate prior automation expectations

## Release Types

- `pre-release`
  - Use when validating support for a new game build
  - Use when runtime entry, Harmony patch layout, or core gameplay-affecting behavior changed materially
  - The version itself must carry a SemVer prerelease suffix
- `stable`
  - Use only after smoke tests pass on the exact supported game build
  - Use only when cold start, mod load, settings persistence, overlay open, and core gameplay-affecting features have been checked
- `hotfix`
  - Bump `PATCH`
  - Use for release regressions in packaging, installability, or high-impact runtime behavior

## Compatibility Rules

- Every release declares one exact supported game build.
- If the game updates before the mod is revalidated, cut the next version as a pre-release first.
- Do not publish a stable release until `TrainerConstants` matches tested reality exactly.

## Compatibility Matrix

Use this shape for release notes and release discussions:

| Mod Version | Platform / Store | STS2 Version | Commit | Status | Notes |
| --- | --- | --- | --- | --- | --- |
| 0.1.x | Windows Steam | v0.99.1 | 7ac1f450 | Verified | Safe Mode fully enabled |

Allowed status values:

- `Verified`
- `Experimental`
- `Unsupported`

## Changelog Contract

Every release entry in `CHANGELOG.md` must contain:

- `### Compatibility`
- `### Added`, `### Changed`, or `### Fixed` as needed
- `### Breaking Changes`

`### Compatibility` must state:

- supported game build
- platform / store
- compatibility status

Use `None.` when there are no breaking changes.

## Maintainer Flow

1. Update the manifest version in `src/Sts2Trainer.Mod/Sts2Trainer.json`.
2. Update the supported game build constants when compatibility changes.
3. Add the new changelog entry with compatibility and breaking-change sections.
4. Commit the release candidate to a clean worktree.
5. Make sure CI has passed for that exact commit.
6. Run `scripts/Test-RepositoryState.ps1`.
7. Run `scripts/Invoke-ReleaseBuild.ps1 -Version <manifest-version>`.
8. Review the generated archive, checksum file, and release notes.
9. Trigger the draft release workflow from `main` or `hotfix/*`.
10. Publish the draft only after install and smoke-test checks pass.

## Release Assets

- Main package:
  - `sts2-trainer-v<mod-version>-win-steam-sts2-<game-version>-<game-commit>.zip`
- Optional symbols package:
  - `sts2-trainer-v<mod-version>-symbols.zip`
- Separate checksum asset:
  - `SHA256SUMS.txt`

The main package contains:

- `Sts2Trainer/Sts2Trainer.dll`
- `Sts2Trainer/Sts2Trainer.json`
- `README.md`
- `LICENSE.txt`

GitHub's automatic `Source code (zip/tar.gz)` archives are not installable mod packages.

## Runner Requirement

The release workflow must run on a Windows self-hosted runner that already has:

- `Slay the Spire 2`
- `.NET SDK 9`
- `GitHub CLI`

The workflow validates these prerequisites before it attempts a build.
