<p align="center">
	<a href="https://github.com/rewalo/BeanModManager/releases"><img alt="GitHub Downloads" src="https://img.shields.io/github/downloads/rewalo/BeanModManager/total?label=Github%20downloads"></a>
	<a href="https://www.curseforge.com/among-us/all-mods/bean-mod-manager"><img alt="Curseforge Downloads" src="https://cf.way2muchnoise.eu/1390152.svg"></a>
</p>

# Bean Mod Manager

A comprehensive mod manager for the popular game Among Us built with Windows Forms. Easily install, manage, and switch between mods like TOHE, Town of Us Mira, Better CrewLink, The Other Roles, and many more without hassle.

![Bean Mod Manager](https://raw.githubusercontent.com/rewalo/BeanModManager/refs/heads/master/Images/Screenshot.png)

## Features

- **First Launch Wizard** - Guided setup for first-time users
- **Automatic Detection** - Auto-detects Among Us installation (Steam, Epic Games, Microsoft Store)
- **Smart Mod Management** - Install multiple mods, but only one active at a time
- **Dependency Management** - Automatically installs required frameworks (Reactor, MiraAPI)
- **Update Checking** - Notifies you when mod updates are available
- **Incompatibility Detection** - Warns about conflicting mods before installation
- **Search & Filter** - Find mods quickly with search and category filters
- **Bulk Operations** - Install, uninstall, or update multiple mods at once
- **Theme Support** - Light and dark themes with system preference detection
- **Virtualized UI** - Optimized performance for large mod lists
- **Steam Depot Support** - Automatic game file downloads for mods that require specific game versions

## How to install?

1. Download the latest release from the [releases page](https://github.com/rewalo/BeanModManager/releases)
2. Run the `BeanModManager-[Version]-Installer.msi` installer
   - The installer will guide you through the setup process
   - You can choose to create desktop and start menu shortcuts
   - Installation requires administrator privileges
3. Launch Bean Mod Manager from the desktop shortcut or Start Menu
4. Complete the first launch wizard:
   - The wizard will help you detect your Among Us installation
   - Select your game version (Steam/Itch.io or Epic/MS Store)
   - Install BepInEx (required for mods to work)
5. You're ready to go!

## How to use it?

- Launch Bean Mod Manager
- Go to the **Mod Store** tab to browse available mods
- Click **Install** on any mod you want
- Once installed, go to the **Installed Mods** tab
- Click **Play** to launch Among Us with that mod active
- Use **Launch Vanilla** to play without mods

## Included Mods

### Featured Mods
- **Town of Host Enhanced (TOHE)** - A host-only modpack for Among Us with enhanced features
- **Town of Us Mira** - Town of Us Reactivated, but cleaner using MiraAPI with many improvements!
- **Better CrewLink** - Voice proximity chat for Among Us
- **The Other Roles GM IA** - A mod for Among Us which adds many new roles, new Settings and new Custom Hats to the game (GM IA version)
- **Endless Host Roles** - The largest Among Us mod

### Gameplay Mods
- **Launchpad: Reloaded** - A vanilla oriented fun and unique Among Us client mod
- **All The Roles** - A mod for Among Us which adds many new roles, modifiers, game modes, map settings, hats and more
- **The Other Roles** - A mod for Among Us which adds many new roles, new Settings and new Custom Hats to the game
- **StellarRoles** - A mod for Among Us which adds new Roles & Modifiers, new Settings and new Custom Cosmetics to the game
- **Las Monjas** - A mod for Among Us which adds new roles and features to the game
- **yanplaRoles** - A mod that introduces new roles and modifiers
- **NewMod** - A mod for Among Us that introduces a variety of new roles, unique abilities, and custom game modes
- **CrowdedMod** - Unlocks the possibility for more than 15 players to join in an Among Us lobby
- **ChaosTokens** - Addon for ToUM that adds chaos! The mod adds Town Of Salem 2 inspired Chaos Tokens to Town of Us Mira
- **Extreme Roles** - AmongUs Mod that adds +100 roles, +1300 options, and cosmetics

### Host Mods
- **Project Lotus: Continued** - Unique, beautiful, and customizable host mod. Adds cosmetics, commands and hotkeys, and various options
- **Minimum Level** - Automatically kick/ban anyone who doesn't fall in range of whatever config you set (Must be host!!)

### Fun Mods
- **SmolMod** - An Among Us Mod that makes players the size of pets!
- **PropHunt** - A mod for Among Us which extends Hide and Seek into a new mode, Prop Hunt!
- **Cursed Among Us** - A mod for Among Us that introduces challenging modifications to gameplay

### Map Mods
- **Unlock dleks ehT** - Mod for Among Us that allows you to play on "dleks ehT" (mirrored Skeld)
- **Submerged** - A mod for Among Us which adds a new map into the game
- **Better Polus** - An Among Us mod that tweaks Polus, allowing a more balanced experience
- **Level Imposter** - Custom Among Us Mapping Studio

### Utility Mods
- **Better-BCL** - A fork of Better CrewLink with some extra features
- **AUnlocker** - A mod that unlocks additional content and features in Among Us
- **Impostor** - The first working Among Us private server, written in C#
- **Vanilla Enhancements** - An among us mod adding lots of quality-of-life features to the game

**Note:** Epic/MS Store versions are automatically detected - mod dropdowns will prefer the right version for your game. Some mods require Steam depot downloads for specific game versions, which are handled automatically.

## Core Frameworks & Dependencies

Bean Mod Manager automatically manages essential modding frameworks that power many of the mods above. These are installed automatically when needed:

- **Reactor** - A shared modding framework that powers most Among Us mods. The manager handles version conflicts automatically.
- **Mira API** - A modern modding API that powers newer mods with enhanced features and better performance

## Requirements

- **Windows 10 or later**
- **.NET Framework 4.8.1 or later**
- **Among Us installed** (Steam, Epic Games Store, or Microsoft Store)
- **Internet connection** for downloading mods and updates

## Making a mod compatible with Bean Mod Manager

To add your mod to Bean Mod Manager, follow these steps:

1. **Publish a release** in a public GitHub repository for your mod
2. In the release, include either (or both):
   - The `.dll` file(s) of the mod
   - A `.zip` file with the Among Us modded directory structure
3. **Follow standard BepInEx structure** - mods should be compatible with BepInEx plugin system
4. **For multiple game versions** (Steam/Epic), include separate assets with naming conventions:
   - **Steam/Itch.io**: Recommended to include "steam" or "itch" in the asset name, or specify in your request
   - **Epic/MS Store**: Recommended to include "epic" or "msstore" in the asset name, or specify in your request
5. **Open an issue or pull request** on this repository with:
   - Your repository link
   - Mod name, author, description
   - Category (Mod, Host Mod, Utility, etc.)
   - Any dependencies or incompatibilities

The mod will be added to the registry after review!

## Building from source

### Prerequisites
- Visual Studio 2019 or later (with .NET Framework 4.8.1 development tools)
- NuGet package manager

### Steps
1. Clone the repository:
   ```bash
   git clone https://github.com/rewalo/BeanModManager.git
   cd BeanModManager
   ```
2. Open `BeanModManager.sln` in Visual Studio
3. Restore NuGet packages (right-click solution → Restore NuGet Packages)
4. Build the solution (Build → Build Solution or F6)
5. Run the application (Debug → Start Debugging or F5)

The built executable will be in `bin\Release\` or `bin\Debug\` depending on your configuration.

## Credits & Resources

Thanks to all the mod creators whose work makes this manager possible. You can access each mod's GitHub repository directly from within Bean Mod Manager by clicking the GitHub link on any mod card.

### Contributing

- **Want your mod added?** Open a GitHub issue or pull request with your repository details (see "Making a mod compatible" section above)
- **Want your mod removed?** Open a GitHub issue or send a message - it'll be removed immediately
- **Found a bug or have a feature request?** Please open an issue on GitHub

## License

This software is distributed under the GNU GPLv3 License. See the [LICENSE](LICENSE) file for details.

## Disclaimer

This application is not affiliated with Among Us or Innersloth LLC, and the content contained therein is not endorsed or otherwise sponsored by Innersloth LLC.