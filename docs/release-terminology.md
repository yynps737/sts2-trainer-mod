# Release Terminology

## Core Terms

- `release`: a published mod package, release notes, and related metadata
- `version`: the mod version number only
- `build compatibility`: the compatibility statement between one mod release and one exact game build
- `supported game build`: the exact game version and commit validated for the release
- `pre-release`: a version that carries a SemVer prerelease suffix such as `-beta.1` or `-rc.1`
- `hotfix`: a patch release cut to correct a regression in an already published version
- `breaking change`: a change that requires manual migration or invalidates an older install, settings file, or workflow

## Naming Rules

- `Release` only refers to mod publication.
- `Version` only refers to mod version numbers.
- `Build` refers to a concrete game build, typically `version + commit`.
- Internal mod versions use bare `SemVer`, for example `0.1.0`.
- Tags and release titles use a `v` prefix, for example `v0.1.0`.
- Pre-release versions use SemVer suffixes such as `v0.2.0-beta.1` and `v0.2.0-rc.1`.
- Hotfix releases use normal patch increments, for example `v0.1.1`.
- Release notes must keep a dedicated `Breaking Changes` section whenever compatibility is broken.
