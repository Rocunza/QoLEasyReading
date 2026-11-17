# Changelog

## v1.1.0 — S3-QC-HF1 packaging update (2026-11-17)

- Fixed Quality Colors (and compatible rich-text label providers) showing literal `<color=...>` markup in duplicate-owned Schematic tooltips. Recognized tags are stripped while the actual owned schematic title and quality label remain.
- Added Debug self-tests for color markup removal, unchanged ordinary titles, and duplicate-title consolidation. The 1.6 and 1.7 versions both passed the 54-test startup audit in provided Debug game logs and were visually confirmed.
- Updated both source projects with the same tested tooltip sanitizer and their own S3-QC-HF1 build IDs.
- **Packaging restriction:** the available 1.6 Release DLL predates the fix and was excluded. The supplied install ZIP is **RimWorld 1.7 only** until a new 1.6 Release is rebuilt and validated.
- Updated the EN/KO About description with the Schematic feature; the runtime package's supportedVersions reflects only the currently shipped 1.7 DLL.

## v1.1.0 — Feature update (2026-11-17)

- Added Schematic research status indicators (`?`, `✓`) and advisory duplicate-ownership warnings (`!`), with verified owned titles in trade tooltips.
- Added trade-session-scoped ownership scanning and safer display policies. Coverage is not universal for foreign mod containers and mixed trade grouping.
- Replaced the wide settings buttons with built-in-looking **Basic / Icons** tabs.
- Separated debug-only developer diagnostics from the Release compilation (Debug 63 C# / Release 55 C#).
- Updated DLL version and startup logging to `v1.1.0` without adding a version suffix to the visible mod title.

## v1.0.0

- Initial release: textbook icons, repeat reading, configurable manual Work-hour reading and EN/KO settings.
