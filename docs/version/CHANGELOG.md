# Changelog

All notable changes to this project will be documented in this file.

## [v0.2.0.0] - 09-16-2025

### Added
- Buttons and logic for refreshing the mods folder, browsing for a different mods folder, and joining the Discord server for our Vintage Story server.  
- An update button within the table for updating individual mods without updating all mods at once.  
- Progress bars and completion dialogs for checking for updates and updating mods.  
- Logic to track how many mods were checked and which mods have updates available.  
- Extended error logging to display informative messages for additional actions that may cause errors.  
- A summary screen after updating mods showing which mods were updated (by `modId`) and their version changes, with a copyable text box for sharing the results.  
- Logic for updating a single mod independently of the full update process.

### Changed
- User interface has been cleaned up for improved usability.  
- Code has been refactored and cleaned for maintainability.

### Fixed
- Corrected links required by the app, including the `update.json` file in the repository and the repository page itself.

---

## [v0.1.0] - 09-14-2025

### Added
- Moved the mods folder textbox to the title bar.
- Enabled click-and-drag of the window via the mods folder textbox.
- "Check for Mod Updates" button with update checking logic.
- "Update All Mods" button with bulk download logic.
- Prompt on launch to select the mods folder.
- HtmlAgilityPack added for scraping mod pages to find versions and download links.
- Single-file publishing enabled.
- Assembly information added to the program.
- MessageService updated with custom dialogs for this app.

### Changed
- Mods table layout updated with a "Latest Version" column.
- Config file location changed to `Documents\AriesLR\VSModUpdateTool`.
- modinfo.json parsing updated to handle both uppercase and lowercase field names.
- Empty fields in the Mods table now display as "N/A".

### Removed
- Folder browse textbox and button.

---

## [v0.0.3] - 09-14-2025

### Added
- Added game version dropdown selection.

- Added a check for mod updates button.

### Changed
- Code cleanup.

---

## [v0.0.2] - 09-14-2025

### Added
- Added logic for handling inconsistent json field naming. (e.g. X mod uses "Name" while Y mod uses "name")

- Added versioning to the project file.

### Changed
- Switched from System.Text.Json to Newtonsoft.Json.Linq, this was mainly due to some mod authors leaving a trailing comma at the end of their modinfo.json causing the system.text JSON parser to fail.

- Increased default app window size

---

## [v0.0.1] - 09-13-2025
### Added
- Initial commit of the project, barebones at this point.
