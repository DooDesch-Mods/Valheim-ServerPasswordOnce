# Valheim - ServerPasswordOnce

> 🛟 **Need help or found a bug?** Get support at [support.doodesch.de/serverpasswordonce](https://support.doodesch.de/serverpasswordonce).

> Server mod: a player who already entered the current server password is not asked for it again.

![Game](https://img.shields.io/badge/game-Valheim%201.0.16-blue)
![BepInEx](https://img.shields.io/badge/BepInEx-5.4.23.3-green)
![Side](https://img.shields.io/badge/side-server%20only-blue)
![Status](https://img.shields.io/badge/status-stable-green)

## What it does

- A player who entered the current password once joins without the password window after that.
- When you change the server password, every player must enter it once more.
- The mod saves no password. For each player it saves the player id, a salted fingerprint of the password
  and the time of the last join.
- When you ban a player, the mod removes that player from the guest list.

## Requirements

| Component | Version |
|---|---|
| Valheim dedicated server | 1.0.16 |
| BepInExPack_Valheim | 5.4.2333 |

The mod works on Steam servers and on servers started with `-crossplay`.

## Installation

1. Install [BepInExPack_Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/) on the server.
2. Put `ServerPasswordOnce.dll` into `BepInEx/plugins/ServerPasswordOnce/` on the server.
3. Start the server with `-password`.

The server log then shows `The server password is in force.` If it shows
`ServerPasswordOnce is not active`, the same line gives the cause.

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
that player is online. For other players, use an id from `list`.

## Guest list

`serverpasswordonce.txt` in the save folder of the server, next to `adminlist.txt`. One line per guest:
player id, password fingerprint, last join. On a crossplay server the platform id follows.

To remove a guest by hand, delete the line and run `serverpasswordonce reload`.

## Crossplay servers

On a server started with `-crossplay`, the player id is the PlayFab player id of the connection. The
PlayFab relay server accepts a connection only with a login that belongs to that id.

The platform id (`Steam_1234`, `Xbox_1234`) is a value that the client sends. The mod does not use it to
skip the password. The mod saves it because `adminlist.txt` and `bannedlist.txt` use it.

- `list` shows both ids. `forget` accepts each of them.
- A guest list from a start without `-crossplay` does not apply with `-crossplay`, and the other way
  around. After such a change, every player must enter the password once more.
- Tested with Steam players. Xbox and PlayStation players use the same connection, but were not tested.

## Configuration

`BepInEx/config/DooDesch.ServerPasswordOnce.cfg` on the server. Restart the server after a change.

| Option | Description | Default |
|---|---|---|
| General / Enabled | When off, every player must enter the password on every join. | true |
| General / LogJoins | Log a line for every join without the password window. The first join of a new guest is always logged. | true |
| Guests / ForgetAfterDays | Remove guests who did not join for this many days. 0 keeps them. | 0 |
| Guests / MaxGuests | Maximum number of guests. When the list is full, the guest with the oldest last join is removed. 0 sets no limit. | 0 |
| Risk / AllowUntrustedBackends | Skip the password on other network backends too, where nothing verifies the player id. No effect on Steam servers and crossplay servers. | false |

## Building (developers)

`dotnet build -c Release` in this folder. References come from `../Workspace/build/GameRefs.props`. The
version is `0.0.0-dev` locally and comes from the release tag in CI.

## License

DooDesch, see `LICENSE.md`. Forks, changes and pull requests are welcome. Publishing or reuploading the mod or a changed copy needs written permission.
