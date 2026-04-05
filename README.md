# STS2 Trainer

`STS2 Trainer` is an in-game utility mod for `Slay the Spire 2`.

The current public build targets the Windows Steam game build `v0.99.1 / 7ac1f450`.

## Supported Build

- Platform / Store: `Windows Steam`
- Supported game build: `v0.99.1 / 7ac1f450`
- Scope: `single-player`
- Default toggle key: `F9`

`Safe Mode` keeps gameplay-affecting features constrained to the verified game build and single-player runs.

## Install

1. Exit the game completely.
2. Extract the release archive.
3. Copy the `Sts2Trainer` folder into:

```text
<Slay the Spire 2>\mods\
```

The final layout should look like this:

```text
<Slay the Spire 2>\mods\Sts2Trainer\Sts2Trainer.dll
<Slay the Spire 2>\mods\Sts2Trainer\Sts2Trainer.json
```

## Upgrade

1. Exit the game completely.
2. Replace the existing `Sts2Trainer` folder with the newer release contents.
3. Launch the game and verify the overlay opens with `F9`.

## Remove

Delete this folder:

```text
<Slay the Spire 2>\mods\Sts2Trainer
```

## Build From Source

Requirements:

- `Slay the Spire 2` installed through Steam
- `.NET SDK 9`

From the repository root:

```powershell
dotnet build .\src\Sts2Trainer.Mod\Sts2Trainer.Mod.csproj
```

The project file checks these default game paths on Windows:

- `D:\SteamLibrary\steamapps\common\Slay the Spire 2`
- `C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2`

If your game lives elsewhere, pass `Sts2Path` to MSBuild.

The optional `.pck` export path also requires a local Godot editor binary. Normal mod builds do not.

## Release Policy

- Mod versions follow `SemVer`: `MAJOR.MINOR.PATCH`
- Tags use `vX.Y.Z`
- Pre-releases carry the SemVer suffix in the version itself, for example `v0.2.0-beta.1`
- Game compatibility is tracked separately from the mod version
- Pushing a release tag is the canonical path for automated GitHub release publication

Maintainer runbook:

- [RELEASING.md](./RELEASING.md)
- [CHANGELOG.md](./CHANGELOG.md)
- [docs/release-terminology.md](./docs/release-terminology.md)
- [docs/self-hosted-runner.md](./docs/self-hosted-runner.md)
- [docs/release-channels.md](./docs/release-channels.md)

## Repository Layout

- `src/Sts2Trainer.Mod`: Godot / C# mod entry, runtime, UI, Harmony patches
- `src/Sts2Trainer.Shared`: shared settings, feature descriptors, constants
- `scripts`: release validation and packaging scripts

## License

This repository is `source-available`, not OSI open source.

It is provided under `PolyForm Noncommercial 1.0.0`. See the official PolyForm terms for the full definition of permitted uses.

- Repository notice: [LICENSE.md](./LICENSE.md)
- Release package notice: `LICENSE.txt`
