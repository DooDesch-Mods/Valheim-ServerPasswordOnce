# Valheim - ServerPasswordOnce

> 🛟 **Need help or found a bug?** Get support at [support.doodesch.de/serverpasswordonce](https://support.doodesch.de/serverpasswordonce).

> Your players type the server password once. After that the window stops appearing, until you change the
> password. Only the server needs the mod; nobody has to install anything.

![Game](https://img.shields.io/badge/game-Valheim%201.0.12-blue)
![BepInEx](https://img.shields.io/badge/BepInEx-5.4.23.3-green)
![Side](https://img.shields.io/badge/side-server%20only-blue)
![Status](https://img.shields.io/badge/status-in%20development-orange)

## Features

- A player who has already given the current password joins without the password window.
- Change the server password and everyone is asked once more. Nothing to reset by hand.
- Server side only. Players keep their vanilla client, and a player with any mod set is treated the same.
- The password is never written to disk. The list holds a player id and a fingerprint of the password,
  salted per file.
- Steam servers only. On the crossplay backend the mod stays out of the way and says so in the log; see
  below.

## Requirements

| Component | Version |
|---|---|
| Valheim | 1.0.12 |
| BepInExPack_Valheim | 5.4.2333 |

No Jötunn, no client mod, no config sync.

## Installation

Put `ServerPasswordOnce.dll` into `BepInEx/plugins/ServerPasswordOnce/` **on the server**. Start the
server with a password as always. That is all.

Do not install it on a client. It does nothing there.

## Configuration

`BepInEx/config/DooDesch.ServerPasswordOnce.cfg` on the server.

| Option | Description | Default |
|---|---|---|
| General / Enabled | Enable the mod. When disabled, every player is asked on every connect, as without it. | true |

## The guest list

The mod writes `serverpasswordonce.txt` next to `adminlist.txt` in the save folder of the server. One line
per player: their platform id and a fingerprint of the password they gave.

The password itself is not in the file. The fingerprint is a hash of the password with a salt that belongs
to that file, so the file is worthless anywhere else. The hash the game keeps cannot be used for this: its
salt is drawn fresh on every server start, so the same password looks different after a restart.

To make the server ask one player again, delete their line and restart the server. To make it ask
everyone, change the password.

## Why Steam only

The mod has to know who is knocking before the password window would appear. It reads that from the
connection, which is the same source the game uses for its ban and admin lists.

On Steam that value comes from the transport and the handshake verifies a session ticket for it, so it is
as trustworthy as a ban.

On the crossplay backend the game takes the same value from a string the client sends, and its own check
accepts every value. Waving a known player through there would let anyone who learns that id join without
the password. The mod therefore does nothing on that backend and writes one line to the log saying so.

## What it changes, in one paragraph

The game asks for a password in two places: the handshake tells the client whether a password is needed,
and the join compares what the client sent. Both read the same field. For a connection that belongs to a
known guest, and only for the length of that one call, the mod presents that field as empty. The client is
told no password is needed, sends an empty one, and it matches. Nothing else about the handshake changes,
and a player who is not on the list goes through the normal path.

## Building (developers)

`dotnet build -c Release` in this folder. References come from `../Workspace/build/GameRefs.props`
(`Workspace/lib/game`, `Workspace/lib/bepinex`). The version is `0.0.0-dev` locally and comes from the
release tag in CI; `[BepInPlugin]` reads it through `DooDesch.ModVersion.Current`.

Debug builds add console commands: `serverpasswordoncestatus`, `serverpasswordoncelist`,
`serverpasswordonceforget <id>`, `serverpasswordonceforgetall`.

## Credits and license

DooDesch. MIT, see `LICENSE.md`.
