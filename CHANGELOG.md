# Changelog

All notable changes to this project are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/); versions follow [SemVer](https://semver.org/).
The section for a version is written once, when the version is tagged; the release workflow
publishes it unchanged to GitHub, Thunderstore and Nexus.

## [Unreleased]

## [1.1.0] - 2026-10-01

- The mod now works on servers started with `-crossplay`. You can keep `-crossplay` in the start command.
- On a crossplay server, `serverpasswordonce list` shows the platform id and the PlayFab player id of each guest. `forget` accepts each of them.
- After a change between a start with `-crossplay` and a start without it, every player enters the password once more.
- Guests that an older version saved on a crossplay server are removed at the first start, because their saved id was not safe.
- `Risk / AllowUntrustedBackends` has no effect on Steam servers and crossplay servers.

## [1.0.1] - 2026-10-01

- The server log says at the start when the mod is not active, and why. On a crossplay server it names `-crossplay` as the argument to remove. Reported by lautasantenni.
- `The server password is in force.` is written only when the mod skips the password for known players.
- The install steps say that `start_headless_server.bat` contains `-crossplay`.

## [1.0.0] - 2026-09-13

- Players who already gave the current server password join without the password window.
- Changing the server password asks everyone once more; the password itself is never stored.
- Admin command `serverpasswordonce` lists, forgets and reloads guests while the server runs.
- Optional: forget a guest after days away, and cap how many guests the server remembers.
- Steam servers by default; crossplay keeps asking unless an admin turns that on in the config.
