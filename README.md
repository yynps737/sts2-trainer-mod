# STS2 Trainer

This repository contains the source for `STS2 Trainer`, an in-game utility mod for `Slay the Spire 2`.

The current implementation targets the Windows Steam build `v0.99.1 / 7ac1f450`. It provides a compact control panel for HP, block, energy, stars, shop overrides, reward shaping, and pacing controls.

## Repository Layout

- `src/Sts2Trainer.Mod`: Godot / C# mod entry, runtime, UI, Harmony patches
- `src/Sts2Trainer.Shared`: shared settings, feature descriptors, constants

## Build

Requirements:

- `Slay the Spire 2` installed through Steam
- `.NET SDK 9`
- `Godot .NET SDK 4.5.1`

From the repository root:

```powershell
dotnet build .\src\Sts2Trainer.Mod\Sts2Trainer.Mod.csproj
```

The project file looks for the game in:

- `D:\SteamLibrary\steamapps\common\Slay the Spire 2`
- `C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2`

If your install path is different, pass `Sts2Path` to MSBuild.

## Install

After a successful build, the mod is copied to:

```text
<Slay the Spire 2>\mods\Sts2Trainer
```

Manual install is also supported. Copy these files into the same folder:

- `Sts2Trainer.dll`
- `Sts2Trainer.json`

## Notes

- Default toggle key: `F9`
- Language follows the game locale when `Auto` is selected
- `Safe Mode` limits gameplay-affecting features to the verified game build and single-player runs

## License

This repository is **source-available**, not OSI open source.

It is provided under `PolyForm Noncommercial 1.0.0`. Commercial use is not permitted.

See [LICENSE.md](./LICENSE.md) for the license notice and official terms URL.
