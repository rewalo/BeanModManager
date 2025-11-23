<p align="center">
	<a href="https://github.com/rewalo/BeanModManager/releases"><img alt="GitHub Downloads" src="https://img.shields.io/github/downloads/rewalo/BeanModManager/total?label=Github%20downloads"></a>
	<a href="https://www.curseforge.com/among-us/all-mods/bean-mod-manager"><img alt="Curseforge Downloads" src="https://cf.way2muchnoise.eu/full_1_downloads.svg"></a>
</p>

# Bean Mod Manager

A simple mod manager for Among Us. Install and manage mods like TOHE, Town of Us Mira, Better CrewLink, and The Other Roles without the hassle.

![Bean Mod Manager](https://raw.githubusercontent.com/rewalo/BeanModManager/refs/heads/master/Images/Screenshot.png)

## How to install?

1. Download the latest release from the [releases page](https://github.com/rewalo/BeanModManager/releases)
2. Extract and run `BeanModManager.exe`
3. Set your Among Us folder path in Settings (or let it auto-detect)
4. Install BepInEx when prompted
5. You're ready to go!

## How to use it?

- Launch Bean Mod Manager
- Go to the **Mod Store** tab
- Click **Install** on any mod you want
- Once installed, click **Play** to launch Among Us with that mod
- Use **Launch Vanilla** to play without mods

Mods are stored separately in `Among Us/Mods/{ModId}/`. When you click Play, it copies the selected mod's files into `BepInEx/plugins/` and launches the game. This way you can have multiple mods installed but only one active at a time.

## Supported Mods

- **Launchpad Reloaded** - A vanilla-oriented Among Us mod with unique features, enhancing the gameplay experience without overcomplicating it.
- **All The Roles** - A mod for Among Us which adds many new roles, modifiers, game modes, map settings, hats and more
- **Town of Host Enhanced (TOHE)** - Host-only modpack with enhanced features
- **Town of Us Mira** - Town of Us Reactivated with MiraAPI
- **Better CrewLink** - Voice proximity chat for Among Us
- **The Other Roles (TOR)** - A mod for Among Us which adds many new roles, new Settings and new Custom Hats to the game
- **Submerged** - A mod for Among Us which adds a new map into the game

Epic/MS Store versions are automatically detected - mod dropdowns will prefer the right version for your game.

## Requirements

- Windows 10 or later
- .NET Framework 4.8.1 or later
- Among Us installed (Steam, Epic, or MS Store)
- Internet connection for downloading mods

## Making a mod compatible with Bean Mod Manager

Publish a release in a public GitHub repository for your mod. In this release, you can add either (or both):

- The `.dll` file of the mod
- A `.zip` with the Among Us modded directory structure

The mod should follow standard BepInEx structure. For multiple game versions (Steam/Epic), include separate assets:
- Steam/Itch.io: Include "steam" or "itch" in the asset name
- Epic/MS Store: Include "epic" or "msstore" in the asset name

Then open an issue or pull request with your repository link and it'll be added!

## Building from source

1. Open `BeanModManager.sln` in Visual Studio
2. Restore NuGet packages
3. Build the solution (F6)
4. Run the application

## Credits & Resources

Thanks to all mod creators. Check their respective GitHub repositories directly in Bean Mod Manager!

If your mod is included and you want it removed, open a GitHub issue or send a message. It'll be removed immediately. Same goes if you want your mod added!

## License

This software is distributed under the GNU GPLv3 License.
