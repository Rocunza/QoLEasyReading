# Release checklist

- [ ] Confirm MIT licensing rights for original source and all 13 icon assets with the maintainer.
- [ ] Build 1.6 Release; verify `Mod/QoLEasyReading/1.6/Assemblies/QoLEasyReading.dll` matches.
- [ ] Build 1.7 Release; verify `Mod/QoLEasyReading/1.7/Assemblies/QoLEasyReading.dll` matches.
- [ ] Check `About/About.xml` supports both versions and requires Harmony.
- [ ] Keep 13 official icons under `Textures/QoLEasyReading/BookSkills` (no replacements).
- [ ] Validate 56 keyed labels per language (Korean, English).
- [ ] Launch 1.6 and 1.7; test trade icon, bookcase, right-click, Ctrl, Alt, manual Work reading ON/OFF.
- [ ] Confirm icon display after switching languages without restart if that behavior is claimed supported.
- [ ] Keep Workshop ID in `About/PublishedFileId.txt`: `3828178548`. Use it only to update this existing Workshop item.
- [ ] Update links in Steam description to the actual GitHub repository once online.
- [ ] Optional wording improvement in `About.xml`: don't imply all vanilla reading behavior is unchanged when Work exception is ON by default.

Steam Workshop: https://steamcommunity.com/sharedfiles/filedetails/?id=3828178548
