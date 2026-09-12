# ServerPasswordOnce

> 🛟 **Need help or found a bug?** Get support at [support.doodesch.de/serverpasswordonce](https://support.doodesch.de/serverpasswordonce).

Your players type the server password once. After that the window stops appearing, until you change the
password. Only the server needs the mod; nobody has to install anything.

## Features

- A player who has already given the current password joins without the password window.
- Change the server password and everyone is asked once more. Nothing to reset by hand.
- Server side only. Players keep their vanilla client.
- The password is never written to disk. The list holds a player id and a fingerprint of the password,
  salted per file.
- Steam servers only. On the crossplay backend the mod stays out of the way and says so in the log.

## Requirements

| Component | Version |
|---|---|
| Valheim | 1.0.12 |
| BepInExPack_Valheim | 5.4.2333 |

No Jötunn, no client mod, no config sync.

## Installation

Put `ServerPasswordOnce.dll` into `BepInEx/plugins/ServerPasswordOnce/` on the server. Start the server
with a password as always. That is all. Do not install it on a client; it does nothing there.

## The guest list

The mod writes `serverpasswordonce.txt` next to `adminlist.txt` in the save folder of the server. One line
per player: their platform id and a fingerprint of the password they gave. The password itself is not in
the file.

To make the server ask one player again, delete their line and restart the server. To make it ask
everyone, change the password.

## Why Steam only

The mod has to know who is knocking before the password window would appear, and it reads that from the
connection, the same source the game uses for its ban and admin lists. On Steam that value comes from the
transport and the handshake verifies a session ticket for it. On the crossplay backend the game takes it
from a string the client sends and accepts every value, so waving a known player through would let anyone
who learns that id join without the password. The mod does nothing on that backend.

## Configuration

`BepInEx/config/DooDesch.ServerPasswordOnce.cfg` on the server.

| Option | Description | Default |
|---|---|---|
| General / Enabled | Enable the mod. When disabled, every player is asked on every connect, as without it. | true |

Source: https://github.com/DooDesch-Mods/Valheim-ServerPasswordOnce
