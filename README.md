# QoL: Easy Reading

A quality-of-life mod for **RimWorld 1.6 and 1.7** by **ZCN**. Adds textbook skill icons, configurable repeat-reading orders, and a manual reading exception during Work schedule hours.

**[한국어 안내 / Korean README](README.ko.md)** · [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3828178548)

## Features

- Show textbook skill icons in **bookcase contents**, **right-click Read menus**, and **trade windows**. Supports one or two skill icons.
- **Ctrl + Left Click** a Read command to queue your preset repeat count.
- **Alt + Left Click** a Read command to open the custom repeat-count dialog.
- Preserve regular Read and vanilla **Shift** job queueing.
- Allow *manually ordered* reading to continue during a **Work** schedule (default **ON**, optional). Automatic reading and other job interruptions retain vanilla rules.
- English and Korean settings, with independently configurable feature switches. **Harmony required; Biotech not required.**

## Installation

Subscribe on [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3828178548), or copy the contents of [`Mod/QoLEasyReading`](Mod/QoLEasyReading) into the RimWorld mod directory as a single `QoLEasyReading` folder. Enable Harmony and then **QoL: Easy Reading** in the mod list.

The mod folder contains separate DLLs under `1.6/Assemblies` and `1.7/Assemblies`, plus shared `Languages`, `Textures`, and `About` resources. Do **not** enable the old combined ReadAndLearnQoL mod alongside this one.

## Source / building

- [`Source/1.6/QoLEasyReading`](Source/1.6/QoLEasyReading): .NET Framework 4.7.2 / Visual Studio 2022 / Harmony 2 / RimWorld 1.6.
- [`Source/1.7/Projects/QoLEasyReading`](Source/1.7/Projects/QoLEasyReading): .NET 10 / Visual Studio 2026 / Harmony 3 / RimWorld 1.7 Waylith.
- See [`BUILDING.md`](BUILDING.md) for local reference setup. Game DLLs and Harmony are **not included**.

## Compatibility notes

- Adding the mod to an existing save is supported for its own book features; create a save backup before changing any mod list.
- Previously generated book titles may stay in the language used when the books were created. This behavior also occurs without Easy Reading.
- The textures in `Textures/QoLEasyReading/BookSkills` are the maintainer-selected **13 original PNG assets**; do not substitute older development icon files.

## License

**MIT License.** Full text: [`LICENSE`](LICENSE). This license applies to the original code and repository assets for which the maintainer owns or controls the required rights; third-party works retain their own licenses. The maintainer must confirm that all 13 included icon assets may be distributed under MIT. RimWorld and Harmony are third-party dependencies and are not bundled here.

© 2026 ZCN.
