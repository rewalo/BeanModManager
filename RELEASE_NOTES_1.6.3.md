## Release 1.6.3

### Added
* **Modpack profiles**: each modpack now lives in its own isolated folder, so switching packs no
  longer requires cleaning and recopying the shared plugins directory on every launch.
* **Duplicate modpacks**: right-click any modpack and choose **Duplicate modpack** to create an
  independent copy with the same mods.
* **Export modpacks**: save a modpack to a `.beanpack` file to back it up or share it with others.
* **Import modpacks**: load a `.beanpack` file to add someone else's pack to your library; imported
  packs get a fresh ID and a unique name to avoid collisions.
* **Cleaner Modpacks UI**: compact icon buttons for creating and importing packs, and the mod count
  is now shown in the title bar.

### Fixed
* Leftover modpack links in the game folder are cleaned up automatically on startup and when the
  app closes, preventing "location not found" errors.
* Uninstalling mods no longer breaks when a modpack link is missing or stale.
* Cleanup is skipped while Among Us is running so the game isn't interrupted.

### Improved
* Existing modpacks are migrated automatically to the new profile format the first time you run
  1.6.3.
* Steam depot, Epic Games Store, and Microsoft Store launch paths continue to work as before.

**Full Changelog**: [v1.6.2...v1.6.3](https://github.com/rewalo/BeanModManager/compare/v1.6.2...v1.6.3)

Happy modding! ❤️
