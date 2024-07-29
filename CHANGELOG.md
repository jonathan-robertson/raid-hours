# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.0.0] - TBH

- remove journal tips; deprecated in 1.0
- update references for 7dtd-1.0-b333

## [1.0.1] - 2023-06-30

- update to support a21 b324 (stable)

## [1.0.0] - 2023-06-25

- add journal entry on login
- update readme
- update to a21 mod-info file format
- update to a21 references

## [0.5.0] - 2023-05-21

- add admin command to get current server time
- add contextual ui text for bag drop mode
- drop bag on logout in hostile lcb
- eject when logging into hostile lcb
- fix startup triggers for new local game
- update journal entry for bag drop mode

## [0.4.0] - 2023-05-17

- unload striking non-player outside of blood moon
- warp away from lcb only during blood moon

## [0.3.0] - 2023-05-16

- add color/info to journal entry
- add notification when ejected for mob protection
- detect if owners/allies are within land claim
- ignore expired land claims
- remove raid protection feature

## [0.2.4] - 2023-05-14

- add admin command to fix raid state for player

## [0.2.3] - 2023-03-24

- fix issue where raid protection would not work
  - logic mistake when checking isSpectator
- update admin console help text for `rh` command

## [0.2.2] - 2023-03-23

- avoid processing spectating players
- fix ally ejection issue

## [0.2.1] - 2023-03-09

- add journal entries explaining features
- fix lcb id check on login

## [0.2.0] - 2023-03-06

- add new toolbelt tip when removed on login
- warp players out of claimed land on login

## [0.1.0] - 2023-03-05

- add admin command to toggle debug logging
- add admin commands to update start/end time and timezone
- add buff to raid protection state with instructions
- add mechanic to trigger raid protection
- add particle effects and sound on warp
- add scheduled lcb defense for build/raid times
- adjust formatting/colors of build/raid/protect panels
- prevent warp from happening for spectator
