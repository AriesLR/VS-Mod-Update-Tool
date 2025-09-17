# VS Mod Update Tool (WIP Name)

[![Changelog](https://img.shields.io/badge/changelog-latest-blue)](docs/version/CHANGELOG.md)

## Table of Contents

- [How It Works](#how-it-works)
- [Requirements](#requirements)
  - [Software](#software)
  - [OS Support](#os-support)
- [Features](#features)
- [Planned Features](#planned-features)
- [Known Issues](#known-issues)
- [Installation](#installation)
  - [How To Use](#how-to-use)
- [Updating](#updating)
- [Screenshots](#screenshots)
- [Acknowledgements](#acknowledgements)
- [License](#license)
- [Tips](#tips)

## How It Works
When the user launches the app, they are prompted to select their mod folder. The app scans every `modNameHere.zip` in the folder, reading the `modinfo.json` inside each archive to populate the main table with relevant information. Once the user clicks "Check for Updates," the app scrapes the mods webpage for version and game compatibility data, storing download links in a `ModLinks.json` file indexed by each mod's `modId`. The table is then updated with the latest version information and a "Has Update" indicator. When the user chooses "Download All Mods," the app downloads any mods that have updates, replacing the old files and keeping track of changes. At the end, it provides a summary listing each mod updated and the version changes that occurred.


## Requirements

### Software

- [.NET 8.0 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
  - This is probably already installed on your system.

### OS Support

- Windows 10/11
  - Older versions of windows may still work.


## Features
- **Automatic Mod Scanning:** Reads every mod in your selected folder and extracts key information from each `modinfo.json`.
- **Update Checking:** Checks online sources for the latest mod versions and compares them to your installed mods.
- **Version Tracking:** Keeps track of which mods have updates available and highlights them in the main table.
- **Batch Downloads:** Download all mods with available updates at once, automatically replacing old versions.
- **Individual Mod Updates:** Download and update a single mod at a time, even if multiple updates are available.
- **Persistent Download Links:** Stores mod download links in a `ModLinks.json` file for faster future updates.
- **Detailed Update Summary:** Provides a clear summary of which mods were updated and their version changes.
- **Easy-to-Use Interface:** Populates a clear, sortable table of your mods, making it easy to see which mods need attention.


## Planned Features

- Ability to paste multiple links to mod pages into the app to automatically build the mod list. This would be useful for adding mods on a fresh server install based on a list of mod page links you already have saved.

## Known Issues

- None at the moment, I need more people using the app to know for sure.

## Installation

Getting started with **VS-Mod-Update-Tool** is simple — just download the [Latest Release](https://github.com/AriesLR/VS-Mod-Update-Tool/releases/latest) and run the `VS-Mod-Update-Tool.exe`. No installation or extraction required, the app is fully portable and can be run from anywhere on your computer.

### How To Use

1. Launch `VS-Mod-Update-Tool.exe`.

2. Browse for your **Mods** folder when prompted.

3. The app will scan all `modNameHere.zip` files and read their `modinfo.json` files to populate the mod table.

4. Click **Check for Updates** to see which mods have new versions available.

5. Click **Download All Mods** to automatically update any mods with available updates.


## Updating

Updating **VS-Mod-Update-Tool** is easy. Since the app is fully portable, all you need to do is download the [Latest Release](https://github.com/AriesLR/PakMaster/releases/latest) and replace your existing `VS-Mod-Update-Tool.exe` with the new one.

Your mod folder selections, saved links, and other settings are stored separately, so they will remain intact after updating.


## Screenshots

### VS Mod Update Tool
![VS Mod Update Tool](https://raw.githubusercontent.com/AriesLR/VS-Mod-Update-Tool/refs/heads/main/docs/img/VS-Mod-Update-Tool.png)

### VS Mod Update Tool Info
![VS Mod Update Tool Info](https://raw.githubusercontent.com/AriesLR/VS-Mod-Update-Tool/refs/heads/main/docs/img/VS-Mod-Update-Tool-Info.png)

 
## Acknowledgements
- [Repak](https://github.com/trumank/repak) - For the Unreal Engine .pak file library and CLI in rust.
    - [unpak](https://github.com/bananaturtlesandwich/unpak) - (Used by Repak) Original crate featuring read-only pak operations.
    - [rust-u4pak](https://github.com/bananaturtlesandwich/unpak) - (Used by Repak) rust-u4pak's README detailing the pak file layout.
    - [jieyouxu](https://github.com/jieyouxu) - (Used by Repak) for serialization implementation of the significantly more complex V11 index.

- [ZenTools](https://github.com/LongerWarrior/ZenTools) - For the Tools for extracting cooked packages from the IoStore container files.
  - [LongerWarrior](https://github.com/LongerWarrior/) - Special thanks to LongerWarrior for maintaining ZenTools.

- [Buckminsterfullerene02](https://github.com/Buckminsterfullerene02/) - For the [UE Modding Tools](https://github.com/Buckminsterfullerene02/UE-Modding-Tools/) databank and contributing to the [UE Modding Guide](https://github.com/Dmgvol/UE_Modding#ue45-modding-guides).
  - [elbadcode](https://github.com/elbadcode) - For contributing to the UE Modding Tools databank.
  - [spuds](https://github.com/bananaturtlesandwich) - For contributing to the UE Modding Tools databank.

- [Dmgvol](https://github.com/Dmgvol/) - For the [UE Modding Guide](https://github.com/Dmgvol/UE_Modding#ue45-modding-guides).

- [Narknon](https://github.com/narknon) - For helping me understand how UE Modding works regarding IoStore Files.
  - Link may be wrong, our conversations took place on discord, but I think this is their GitHub.

- [1Armageddon1](https://github.com/1armageddon1) - For being the OG tester for the tool as well as helping give insight on how the tool should work. Wouldn't be nearly as far as I am progress-wise without them.


## License

[MIT License](LICENSE)

## Tips
[Buy Me a Coffee](https://www.buymeacoffee.com/arieslr)

<img src="https://i.imgflip.com/1u2oyu.jpg" alt="I like this doge" width="100">