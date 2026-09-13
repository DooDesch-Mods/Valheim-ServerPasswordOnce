# Valheim - ServerPasswordOnce

> 🛟 **Need help or found a bug?** Get support at [support.doodesch.de/serverpasswordonce](https://support.doodesch.de/serverpasswordonce).

> Server mod: a player who already entered the current server password is not asked for it again.

![Game](https://img.shields.io/badge/game-Valheim%201.0.12-blue)
![BepInEx](https://img.shields.io/badge/BepInEx-5.4.23.3-green)
![Side](https://img.shields.io/badge/side-server%20only-blue)
![Status](https://img.shields.io/badge/status-stable-green)

## What it does

- A player who entered the current password once joins without the password window after that.
- When you change the server password, every player must enter it once more.
- The mod saves no password. For each player it saves the Steam id, a salted fingerprint of the password
  and the time of the last join.
- When you ban a player, the mod removes that player from the guest list.

## Requirements

| Component | Version |
|---|---|
| Valheim dedicated server | 1.0.12 |
| BepInExPack_Valheim | 5.4.2333 |

The mod works on Steam servers. On a server started with `-crossplay` it asks every player as usual, see
[Crossplay servers](#crossplay-servers).

## Installation

1. Install [BepInExPack_Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/) on the server.
2. Put `ServerPasswordOnce.dll` into `BepInEx/plugins/ServerPasswordOnce/` on the server.
3. Start the server with `-password` as usual.

The server log then shows `The server password is in force.`

## Admin command

Admins from `adminlist.txt` type the command in the game console. The game runs it on the server.

```
serverpasswordonce status            mod state, backend, number of guests, path of the guest list
serverpasswordonce list              all guests with their last join
serverpasswordonce forget <id|name>  remove one guest; they must enter the password again
serverpasswordonce forgetall         remove all guests
serverpasswordonce reload            read the guest list file again
```

`status` and `list` write to the server log, not to your game console. `forget <name>` works only while
that player is online. For other players, use the id from `list`.

## Guest list

`serverpasswordonce.txt` in the save folder of the server, next to `adminlist.txt`. One line per guest:
Steam id, password fingerprint, last join in UTC.

To remove a guest by hand, delete the line and run `serverpasswordonce reload`.

## Crossplay servers

On a server started with `-crossplay`, the game does not verify the player id. Anyone who knows the id of
a guest could join without the password. For this reason the mod does not skip the password there and
writes a warning to the server log.

`Risk / AllowUntrustedBackends = true` skips the password on crossplay servers too. While it is on, the
server writes a warning on every start.

## Configuration

`BepInEx/config/DooDesch.ServerPasswordOnce.cfg` on the server. Restart the server after a change.

| Option | Description | Default |
|---|---|---|
| General / Enabled | When off, every player must enter the password on every join. | true |
| General / LogJoins | Log a line for every join without the password window. The first join of a new guest is always logged. | true |
| Guests / ForgetAfterDays | Remove guests who did not join for this many days. 0 keeps them. | 0 |
| Guests / MaxGuests | Maximum number of guests. When the list is full, the guest with the oldest last join is removed. 0 sets no limit. | 0 |
| Risk / AllowUntrustedBackends | Skip the password on crossplay servers too. See "Crossplay servers". | false |

## Building (developers)

`dotnet build -c Release` in this folder. References come from `../Workspace/build/GameRefs.props`. The
version is `0.0.0-dev` locally and comes from the release tag in CI.

## License

DooDesch. All rights reserved, see `LICENSE.md`. You may download the mod and run it on your server.
Changing, reusing or redistributing it needs written permission.
