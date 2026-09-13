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
- Steam servers by default. On the crossplay backend the mod stays out of the way and says so in the log,
  because the player id cannot be trusted there; one switch overrides that, see below.
- One admin command to take a player off the list, without changing the password for everyone else.
- Optional: forget a guest who has been away for too long, and cap the length of the list.

## Requirements

| Component | Version |
|---|---|
| Valheim | 1.0.12 |
| BepInExPack_Valheim | 5.4.2333 |

No Jötunn, no client mod, no config sync.

## Installation

Put `ServerPasswordOnce.dll` into `BepInEx/plugins/ServerPasswordOnce/` on the server. Start the server
with a password as always. Do not install it on a client; it does nothing there.

## The guest list

The mod writes `serverpasswordonce.txt` next to `adminlist.txt` in the save folder of the server. One line
per player: their platform id, a fingerprint of the password they gave, and their last visit. The password
itself is not in the file.

To make the server ask one player again, use `serverpasswordonce forget`, or delete their line and run
`serverpasswordonce reload`. To make it ask everyone, change the password. A player you ban is taken off
the list on their own.

## Admin command

```
serverpasswordonce status            version, backend, guest count, file, switches
serverpasswordonce list              every guest with a shortened fingerprint and their last visit
serverpasswordonce forget <id|name>  one guest, so they are asked again
serverpasswordonce forgetall         all of them
serverpasswordonce reload            read the guest list again, no restart
```

An admin can type it in their own console and it runs on the server: the game sends it there and checks
`adminlist.txt` itself. A name works for a player who is connected right now; otherwise use the id from
the guest list.

The answer does not come back to you. The game runs a remote command without the connection it came from,
so `status` and `list` print into the server console and the server log.
`forget`, `forgetall` and `reload` do their work either way.

## Why Steam by default

The mod has to know who is knocking before the password window would appear, and it reads that from the
connection, the same source the game uses for its ban and admin lists. On Steam that value comes from the
transport and the handshake verifies a session ticket for it. On the crossplay backend the game takes it
from a string the client sends and accepts every value, so waving a known player through would let anyone
who learns that id join without the password. The mod does nothing on that backend.

`Risk / AllowUntrustedBackends` turns that refusal off, for an admin who decides the password on their
server is a convenience and not a lock. It writes a warning to the log on every start. If the password is
what keeps people out, leave it alone.

## Configuration

`BepInEx/config/DooDesch.ServerPasswordOnce.cfg` on the server.

| Option | Description | Default |
|---|---|---|
| General / Enabled | Enable the mod. When disabled, every player is asked on every connect, as without it. | true |
| General / LogJoins | Write a line for every player who joins without the window. The first time a player is added is always written. | true |
| Guests / ForgetAfterDays | Days without a visit until a guest is forgotten and asked again. 0 keeps them while the password stands. | 0 |
| Guests / MaxGuests | Largest number of guests to keep. Over that, whoever visited longest ago is dropped. 0 sets no limit. | 0 |
| Risk / AllowUntrustedBackends | Skip the password on backends other than Steam as well. Read "Why Steam by default" first. | false |
