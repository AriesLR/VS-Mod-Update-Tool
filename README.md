# VS Mod Update Tool (WIP Name)

[![Changelog](https://img.shields.io/badge/changelog-latest-blue)](docs/version/CHANGELOG.md)

## Table of Contents

- [How It Works](#how-it-works)
- [Requirements](#requirements)
  - [Software](#software)
  - [OS Support](#os-support)
- [Features](#features)
- [Known Issues](#known-issues)
- [Installation](#installation)
  - [How To Use](#how-to-use)
- [Updating](#updating)
- [Screenshots](#infographic)
- [Acknowledgements](#acknowledgements)
- [License](#license)
- [Tips](#tips)

## How It Works  
When the user launches the app, they are prompted to select their mod folder. The app scans every `modNameHere.zip` in the folder, reading the `modinfo.json` inside each archive to populate the main table with relevant information.  

When the user clicks **"Check for Updates,"** the app queries the Vintage Story Mods API for each mod’s latest version and game compatibility data. Any download links retrieved are cached in a temporary `ModLinks.json` file, indexed by `modId` and overwritten each time updates are checked. The table is then refreshed with the newest version information and a **"Has Update"** indicator.  

If the user selects **"Download All Mods,"** the app fetches the updated mods, replaces the old files, and keeps track of changes. At the end of the process, a summary is displayed showing which mods were updated and the version differences.  


## Requirements

### Software

- [.NET 8.0 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
  - This is probably already installed on your system.

### OS Support

- Windows 10/11
  - Older versions of windows may still work.


## Features
- **Automatic Mod Scanning:** Reads every mod in your selected folder and extracts key details from each `modinfo.json`.  
- **Update Checking:** Uses the Vintage Story Mods API to fetch the latest versions and game compatibility data, comparing them against your installed mods.  
- **Version Tracking:** Highlights which mods have updates available directly in the main table.  
- **Batch Updates:** Update and replace all outdated mods at once with the latest versions.  
- **Individual Mod Updates:** Update a single mod independently, even if multiple mods have updates available.  
- **Detailed Update Summary:** Displays a clear summary of all updated mods along with their version changes.  
- **User-Friendly Interface:** Shows your mods in a clean, sortable table, making it easy to spot which ones need updating.  


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

Updating **VS-Mod-Update-Tool** is easy. Since the app is fully portable, all you need to do is download the [Latest Release](https://github.com/AriesLR/VS-Mod-Update-Tool/releases/latest) and replace your existing `VS-Mod-Update-Tool.exe` with the new one.


## Infographic
![VS Mod Update Tool Info](https://raw.githubusercontent.com/AriesLR/VS-Mod-Update-Tool/refs/heads/main/docs/img/VS-Mod-Update-Tool-Info.png)

The infographic might not be 100% up to date as it takes a ton of effort to update any time I change a small thing with the UI. I will update it on larger changes otherwise it still should be mostly correct.

 
## Acknowledgements
- [Vintage Story Devs](https://www.vintagestory.at) - For unintentionally inspiring this whole project. If it wasn't for the game this wouldn't exist.


## License

[MIT License](LICENSE)

## Tips
[Buy Me a Coffee](https://www.buymeacoffee.com/arieslr)

<img src="https://i.imgflip.com/1u2oyu.jpg" alt="I like this doge" width="100">