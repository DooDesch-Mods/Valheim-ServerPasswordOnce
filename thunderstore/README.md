# ServerPasswordOnce

> 🛟 **Need help or found a bug?** Get support at [support.doodesch.de/serverpasswordonce](https://support.doodesch.de/serverpasswordonce).

A starter template for Valheim BepInEx mods without Jötunn. Installed in a game it only logs that it initialized.

## Requirements

| Component | Version |
|---|---|
| Valheim | 0.221.12 |
| BepInExPack_Valheim | 5.4.2333 |

## Installation

Install with a mod manager (r2modman, Gale or Thunderstore Mod Manager), or drop `ServerPasswordOnce.dll` into
`Valheim/BepInEx/plugins/ServerPasswordOnce/` after installing BepInExPack_Valheim. Install it on the server too;
the config is synced from the server.

## Configuration

`BepInEx/config/DooDesch.ServerPasswordOnce.cfg`, editable in game with ConfigurationManager (F1).

| Option | Description | Default |
|---|---|---|
| General / LockConfiguration | When enabled on the server, clients use the server's values and cannot change them. | true |
| General / Enabled | Enable the mod. | true |

Source: https://github.com/DooDesch-Mods/Valheim-ServerPasswordOnce
