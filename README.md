# Raid Hours

[![🧪 Tested On 7DTD 1.1 (b14)](https://img.shields.io/badge/🧪%20Tested%20On-7DTD%201.1%20(b14)-blue.svg)](https://7daystodie.com/) [![📦 Automated Release](https://github.com/jonathan-robertson/raid-hours/actions/workflows/release.yml/badge.svg)](https://github.com/jonathan-robertson/raid-hours/actions/workflows/release.yml)

TODO: ![raid-hours social image](https://raw.githubusercontent.com/jonathan-robertson/raid-hours/media/raid-hours-logo-social.jpg)

- [Raid Hours](#raid-hours)
  - [Summary](#summary)
    - [Support](#support)
  - [Features](#features)
    - [Scheduled Claim Defense](#scheduled-claim-defense)
    - [Mob Raid Protection](#mob-raid-protection)
    - [Squatting/Trap Protection](#squattingtrap-protection)
    - [Bag Drop Mode](#bag-drop-mode)
  - [Admin Commands](#admin-commands)
  - [Compatibility](#compatibility)

## Summary

Real people have real lives; disable raiding while most players are at work or asleep.

### Support

🗪 If you would like support for this mod, please feel free to reach out via [Discord](https://discord.gg/tRJHSB9Uk7).

## Features

A server running Raid Hours will have some special features related to Land Claims...

### Scheduled Claim Defense

The currently active Claim Mode is displayed as a persistent buff. Select this buff in your character sheet for more info.

Claim Mode | Description
--- | ---
**Build Mode** | Claimed Land remains protected from hostile player damage.
**Raid Mode** | Land Claim Defense drops to enable pvp raiding between the hours set by the admin.

### Mob Raid Protection

When the owner and all allies are absent, Land Claims **prevent damage** from all zombies/animals and despawn them upon striking a block.

While during **Blood Moon**, zombies, animals, and players are instead warped away from the land claim if blocks are attacked a zombie or animal.

### Squatting/Trap Protection

Players who log into a hostile land claim's range will be automatically warped out of range.

This prevents one player from 'jailing' another and also provides base builders with confidence that enclosed goodies will remain safe during **Build Mode**.

This feature is always on, regardless of which Land Claim Mode is active.

### Bag Drop Mode

Within a hostile land claim, logging out or disconnecting will cause you to drop your backpack.

Any time you're in this situation, viewing the ESC menu will display a warning to let you know.

## Admin Commands

Each of options would be called with the command `raidhours` or `rh`:

- `debug`: toggle debug logging mode
- `fix <user id / player name / entity id>`: fix player's raid hours state; if player is unable to damage claimed land during raid hours, this will re-send the correct raid mode values to the given player
- `settings`: show raid-hours mod settings
- `gt`: get current, real-world server time
- `timezones`: list server-side timezones for your server's operating system
- `set timezone <string>`: set the timezone; use 'list' to get a list of timezones your operating system supports
- `set <start/stop> [d=Monday/Tuesday/...] [h=value] [m=value]`: update the start or stop time with the provided rule... d (day of week), (h hour of day), and m (minute of hour) can all be omitted, but m will default to 0 (i.e. top of the hour). NOTE: h is in 24-hr time, so 17 = 5pm.

## Compatibility

Environment | Compatible | Does EAC Need to be Disabled? | Who needs to install?
--- | --- | --- | ---
Dedicated Server | Yes | No | only server
Peer-to-Peer Hosting | No | No | N/A
Single Player | No | No | N/A

> TODO: maybe one day, this mod will be updated to support P2P and SP
