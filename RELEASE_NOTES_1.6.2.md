## Release 1.6.2

### Fixed

- **Epic/MS Store launches**: MS Store (Xbox) installs were incorrectly launched through EpicGamesStarter, causing an Epic Games ownership check to fail for MS Store owners. The launcher now detects Epic (`EGS` marker) and MS Store (`Win10` marker) installs separately — Epic uses EpicGamesStarter, MS Store launches via `shell:AppsFolder` protocol activation like the Start Menu does.
- **Steam check**: Steam is now only required when the install is actually a Steam copy (path under `steamapps`). itch.io installs no longer demand Steam.
- **Town of Us Mira**: the Mod Browser's "DLL Only" option pointed at `MiraAPI.dll` (a dependency), so installing it produced a broken install with no roles. The browser now offers the full release zips, matching the Custom Import behavior.
- **Level Imposter**: downloaded maps stored in `BepInEx/plugins/LevelImposter` were deleted on every launch. Mod `keepFiles` folders are now preserved during pre-launch cleanup.

### Added

- **New mods**: TechTech's Sound Mod, Divani Mods, Perfect Comms, NotePad Mod.
- **Better Among Us**: moved to the actively maintained repository (`D1GQ/BetterAmongUs`), restoring support for current Among Us versions.
- **Debug builds**: added a Debug menu with "Load Mod Store" (mod store no longer auto-loads in debug builds, avoiding GitHub rate limits) and "Populate Mod Registry Cache" (writes `mod-cache.json` incrementally and resumes after rate limits). Debug builds can also skip the game-path wizard step.
- **Releases**: portable EXEs and installers are now built for x86, x64, and ARM64.

**Full Changelog**: https://github.com/rewalo/BeanModManager/compare/v1.6.1...v1.6.2

Happy modding! ❤️
