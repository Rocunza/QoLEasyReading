# QoL: Easy Reading — v1.1.0 (Quality Colors hotfix)

By **ZCN**. **Source projects:** RimWorld 1.6 + 1.7. **Current downloadable binary:** RimWorld **1.7 only**. The previously published RimWorld 1.6 Release DLL was built before the Quality Colors fix and is deliberately excluded until the maintainer produces and verifies a new 1.6 Release build.

[한국어 README](README.ko.md) · [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3828178548) · [Changelog](CHANGELOG.md) · [Packaging status](PACKAGING_STATUS.md)

## Features

- Book skill icons in bookcases, right-click reading menus and trade windows.
- Schematic research-state indicators in bookcases, right-click menus and trade windows; an advisory duplicate-ownership warning and owned-title tooltip when trading.
- **Ctrl + Left Click** queues the preset repeat-read count; **Alt + Left Click** opens a count dialog. Ordinary Read and Shift queueing remain vanilla.
- Manually ordered reading can continue during Work-scheduled hours (configurable, default ON).
- Vanilla-style Basic / Icons settings tabs; English and Korean UI.
- **Quality Colors compatibility hotfix:** known rich-text color/style tags are stripped from owned schematic titles in the duplicate tooltip while retaining the real title and quality text.
- Release builds omit Easy Reading's developer-only diagnostics.

## Installation (current GitHub package)

Download **`QoLEasyReading-v1.1.0-RW17-QCHF1.zip`** and extract its `QoLEasyReading/` folder into your RimWorld **1.7** `Mods` folder. Activate Harmony first. Do not activate the legacy combined `ReadAndLearnQoL` alongside this mod.

```text
Mods/QoLEasyReading/
  About/About.xml                # this install package explicitly declares 1.7 only
  1.7/Assemblies/QoLEasyReading.dll
  Languages/
  Textures/
```

**RimWorld 1.6 players:** This ZIP is **not** a working 1.6 package. The complete, hotfix-updated 1.6 source is at [`Source/1.6/QoLEasyReading`](Source/1.6/QoLEasyReading). Build a new 1.6 Release DLL using [BUILDING.md](BUILDING.md), validate it in game, and only then include it in an updated dual-version installer. An older DLL copied over the top would silently reintroduce the original tooltip defect.

## Source and tests

- [RimWorld 1.6](Source/1.6/QoLEasyReading) — .NET Framework 4.7.2, Harmony 2.
- [RimWorld 1.7](Source/1.7/Projects/QoLEasyReading) — .NET 10, Harmony 3, Waylith.
- [Build instructions](BUILDING.md) · [Release checklist](RELEASE_CHECKLIST.md) · [Packaging manifest](RELEASE_MANIFEST.json).

The QC fix was visually verified in Debug gameplay on both versions; the 1.7 Release DLL's hotfix build ID and source were checked. Final Release *in-game* verification remains separate from static checks. No new gameplay Harmony patches were introduced in this packaging pass.

## Limitations and rights

Schematic ownership detection covers known container sources, not all custom containers from third-party mods; mixed/grouped trade directions are not exhaustively proven. Duplicate warnings are advisory, not purchase blockers. `Schematic_Review.png` remains a reserved asset identical to the duplicate-warning image.

The [MIT License](LICENSE) applies only to material the author may license; confirm public redistribution rights for the 13 textbook icons and Schematic textures before publication. Game/Harmony/Unity binaries are not bundled.

© 2026 ZCN.
