# Raid Hours [![Status: 💟 End of Life](https://img.shields.io/badge/💟%20Status-End%20of%20Life-blue.svg)](#support)

[![🧪 Tested with 7DTD 1.3 (b9)](https://img.shields.io/badge/🧪%20Tested%20with-7DTD%201.3%20(b9)-blue.svg)](https://7daystodie.com/)
[![🧪 Tested with 7DTD 1.2 (b27)](https://img.shields.io/badge/🧪%20Tested%20with-7DTD%201.2%20(b27)-blue.svg)](https://7daystodie.com/)
[![✅ Dedicated Servers Supported ServerSide](https://img.shields.io/badge/✅%20Dedicated%20Servers-Supported%20Serverside-blue.svg)](https://7daystodie.com/)
[![❌ Single Player and P2P Unupported](https://img.shields.io/badge/❌%20Single%20Player%20and%20P2P-Unsupported-red.svg)](https://7daystodie.com/)
[![📦 Automated Release](https://github.com/jonathan-robertson/raid-hours/actions/workflows/release.yml/badge.svg)](https://github.com/jonathan-robertson/raid-hours/actions/workflows/release.yml)

## Summary

Real people have real lives; disable raiding while most players are at work or asleep.

> 💟 This mod has reached [End of Life](#support) and will not be directly updated to support 7 Days to Die 2.0 or beyond. Because this mod is [MIT-Licensed](LICENSE) and open-source, it is possible that other modders will keep this concept going in the future.
>
> Searching [NexusMods](https://nexusmods.com) or [7 Days to Die Mods](https://7daystodiemods.com) may lead to discovering other mods either built on top of or inspired by this mod.

### Support

💟 This mod has reached its end of life and is no longer supported or maintained by Kanaverum ([Jonathan Robertson](https://github.com/jonathan-robertson) // me). I am instead focused on my own game studio ([Calculating Chaos](https://calculatingchaos.com), if curious).

❤️ All of my public mods have always been open-source and are [MIT-Licensed](LICENSE); please feel free to take some or all of the code to reuse, modify, redistribute, and even rebrand however you like! The code in this project isn't perfect; as you update, add features, fix bugs, and otherwise improve upon my ideas, please make sure to give yourself credit for the work you do and publish your new version of the mod under your own name :smile: :tada:

## Features

A server running Raid Hours will have some special features related to Land Claims...

### Scheduled Claim Defense

The currently active Claim Mode is displayed as a persistent buff. Select this buff in your character sheet for more info.

| Claim Mode     | Description                                                                        |
| -------------- | ---------------------------------------------------------------------------------- |
| **Build Mode** | Claimed Land remains protected from hostile player damage.                         |
| **Raid Mode**  | Land Claim Defense drops to enable pvp raiding between the hours set by the admin. |

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

| Environment          | Compatible | Does EAC Need to be Disabled? | Who needs to install? |
| -------------------- | ---------- | ----------------------------- | --------------------- |
| Dedicated Server     | Yes        | No                            | only server           |
| Peer-to-Peer Hosting | No         | No                            | N/A                   |
| Single Player        | No         | No                            | N/A                   |

> TODO: maybe one day, this mod will be updated to support P2P and SP
