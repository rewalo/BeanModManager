## Release 1.5.9

### Added

- Two-phase loading system: Installed mods now display immediately while store mods load in the background
- Skeleton loading cards with smooth shimmer animation for better visual feedback during mod loading
- Improved status bar with marquee progress indicator and detailed loading phase messages
- Code refactoring: Split Main.cs into organized partial classes for better maintainability

### Fixed

- Fixed update checker only running when GitHub cache updates (now checks on every app launch)
- Fixed imported custom mods not appearing in installed list until switching tabs
- Fixed status bar showing "You are running the latest version" during mod loading instead of loading progress
- Fixed "No Mods To Browse" message appearing in store while loading (skeleton cards now persist properly)
- Fixed store scrollbar not appearing after mods finish loading
- Fixed skeleton cards being too small (now match mod card height at 250px)
- Fixed text clipping and layout issues on high/low DPI displays and different screen resolutions
- Improved skeleton card shimmer animation for smoother, less jarring visual effect

**Full Changelog**: https://github.com/rewalo/BeanModManager/compare/v1.5.8...v1.5.9

Happy modding! ❤️

