# Valheim - ServerPasswordOnce

> 🛟 **Need help or found a bug?** Get support at [support.doodesch.de/serverpasswordonce](https://support.doodesch.de/serverpasswordonce).

> A starter template for Valheim BepInEx mods without Jötunn: plain BepInEx plus ServerSync merged into the DLL. Clone it, run the rename script, and you have a mod skeleton that builds, deploys into the test copy and already matches the workspace conventions.

![Game](https://img.shields.io/badge/game-Valheim%200.221.12-blue)
![BepInEx](https://img.shields.io/badge/BepInEx-5.4.23.3-green)
![Status](https://img.shields.io/badge/status-template-lightgrey)

This is a developer template, not a player-facing mod. Installed in a game it only logs that it initialized.
Use `Boilerplate` instead when the mod adds items, pieces, locations or wants Jötunn's managers.

## What is in it

- `Core.cs`: the `BaseUnityPlugin` with `[BepInPlugin]` and a `ServerSync.ConfigSync` keyed on the GUID. `Awake` binds the config, applies the Harmony patches of the assembly and logs the version. BepInEx applies nothing on its own; the `PatchAll` here is the one place patches are applied.
- `Config/ServerPasswordOnceConfig.cs`: `ConfigEntry<T>` bound through `Config.Bind` and registered with ServerSync, plus the locking entry that lets the server overrule clients.
- `Patches/ExamplePatch.cs`: a commented Harmony patch. The game assembly is publicized by the build, so private members are reachable by name.
- `Debug/DevCommands.cs`: console commands (F5) registered through `Terminal.ConsoleCommand`, Debug builds only. Dev tooling is console commands, never hotkeys.
- `thunderstore/`: manifest, player README and icon for the Thunderstore package. The icon is a placeholder; replace it before a release.
- `.github/workflows/`: `build.yml` builds Debug and Release on every push, `build-and-release.yml` publishes on a `vX.Y.Z` tag.
- `setup_mod.sh`: renames everything from `ServerPasswordOnce` to your mod name.

## Requirements

| Component | Version |
|---|---|
| Valheim | 0.221.12 |
| BepInExPack_Valheim | 5.4.2333 |

ServerSync is compiled into the mod (ILRepack); players install nothing extra.

## New mod from this template

```sh
cp -r ServerPasswordOnce MyMod && cd MyMod && ./setup_mod.sh
```

Then `dotnet build` (Debug deploys into `_Test\Valheim\BepInEx\plugins\MyMod\`), start the test copy with
`pwsh -File ../Workspace/tools/session/session.ps1 -Build MyMod` and look for `Loading [MyMod 0.0.0]` in
`_Test\Valheim\BepInEx\LogOutput.log`.

## Configuration

`BepInEx/config/DooDesch.ServerPasswordOnce.cfg`, editable in game with ConfigurationManager (F1).

| Option | Description | Default |
|---|---|---|
| General / LockConfiguration | When enabled on the server, clients use the server's values and cannot change them. | true |
| General / Enabled | Enable ServerPasswordOnce. Synced from the server. | true |

## Building

`dotnet build -c Release` in this folder. References come from `../Workspace/build/GameRefs.props`
(`Workspace/lib/game`, `Workspace/lib/bepinex`); `ServerSync.props` adds `Workspace/lib/serversync/ServerSync.dll`
and merges it into the output. The version is `0.0.0-dev` locally and comes from the release tag in CI;
`[BepInPlugin]` reads it through `DooDesch.ModVersion.Current`.

## Credits and license

DooDesch. MIT, see `LICENSE.md`. ServerSync by blaxxun, https://github.com/blaxxun-boop/ServerSync.
