# [Version 1.6.3]

## What's Changed
* Added the option to change game language in dd3258b7feb26089fd9a7b7de71c0381a58b9bd8. ('user_language' option)

## Bugfixes
* Fixed "Internal Display Resolution" option not showing in ff81eb90636d3ca89eecaf18cb3cf528d4f56f33.

**Full Changelog**: https://github.com/xenia-manager/xenia-manager/compare/1.6.2...1.6.3

# [Version 1.6.2]

## What's Changed
* Fix vertical scrolling through Library in 5736d8f
* Added function to uninstall Xenia Stable & Canary in https://github.com/xenia-manager/xenia-manager/pull/40
* Cleaned up Library code, specifically code related to loading games into the UI in https://github.com/xenia-manager/xenia-manager/pull/40 (Now it should be easier to add additional features like Netplay build)

## Bugfixes
* Fixed a bug with searching for games when adding them that would make some games not be recognized due to formatting in https://github.com/xenia-manager/xenia-manager/pull/40

**Full Changelog**: https://github.com/xenia-manager/xenia-manager/compare/1.6.1...1.6.2

# [Version 1.6.1]

## What's Changed
* Added a check and backup options for Andy Decarli's icon grabbing in 0231590

**Full Changelog**: https://github.com/xenia-manager/xenia-manager/compare/1.6.0...1.6.1

# [Version 1.6.0]

## What's Changed
* Use relative instead of absolute paths in the configuration file by @shazzaam7 in https://github.com/xenia-manager/xenia-manager/pull/38 (This should make app fully portable)
* Added a "Reset Configuration" button that will try to fix the configuration file of Xenia Manager in db1c180
* Added a new Dark theme while the old one was renamed to AMOLED in 8c097f4
* Swapped the non checked and checked colors in AMOLED, Dark and Nord theme in 8a8d344

## Bugfixes
* Fixed a bug where Video Settings wouldn't get disabled if they're not in the configuration file in 3fada4a

**Full Changelog**: https://github.com/xenia-manager/xenia-manager/compare/1.5.0...1.6.0

# [Version 1.5.0]

## What's Changed
* Added 'Nord' theme in 129333c
* Added an option to open the selected configuration file in a default text editor (backup option is notepad in case the first one breaks) in 8d18577
* Cleaned up tooltips and context menu text

## Bugfixes
* Fixed 'internal_display_resolution' not being shown in c2a32a0


**Full Changelog**: https://github.com/xenia-manager/xenia-manager/compare/1.4.0...1.5.0

# [Version 1.4.0]

## What's Changed
* Added "Open Compatibility Page" option by @shazzaam7 in https://github.com/xenia-manager/xenia-manager/pull/36 (Have to re add games to Xenia Manager for this to show up)
* Improved searching when adding games to Xenia Manager in d810cae
* If the game isn't found in the lists when adding it, it'll use default disc icon in 83d927a

**Full Changelog**: https://github.com/xenia-manager/xenia-manager/compare/1.3.1...1.4.0

# [Version 1.3.1]

## Bugfixes
* Fixed rounded edges of the game images in e97ba99
**Full Changelog**: https://github.com/xenia-manager/xenia-manager/compare/1.3.0...1.3.1

# [Version 1.3.0]

## What's Changed
* Added caching of WPF Pages by @shazzaam7 in https://github.com/xenia-manager/xenia-manager/pull/32 (This fixes a memory leak and keeps the RAM usage to ~150MB after every WPF Page is opened)
* Added title updates support (Xenia Canary ONLY) by @shazzaam7 in https://github.com/xenia-manager/xenia-manager/pull/33
* Added theoption for switching emulator version the game use by @shazzaam7 in https://github.com/xenia-manager/xenia-manager/pull/34
* Code cleanup by @shazzaam7 in https://github.com/xenia-manager/xenia-manager/pull/35

## Bugfixes
* Fixed downloading game patches in 3ace5e7. Now they will properly install in Xenia Canary patches folder
* Now game patches properly get assigned their location to their respective games. Fixed in 96a6c5f

**Full Changelog**: https://github.com/xenia-manager/xenia-manager/compare/1.2.0...1.3.0

# [Version 1.2.0]

# It is recommended to start fresh with this version since a lot of the changes happened under the hood
## What's Changed
* Theme support (Currently Light and Dark/AMOLED) by @shazzaam7 in https://github.com/xenia-manager/xenia-manager/pull/21
* Add Xenia Stable support by @shazzaam7 in https://github.com/xenia-manager/xenia-manager/pull/23
* Add option to import and export save files by @shazzaam7 in https://github.com/xenia-manager/xenia-manager/pull/25

## Improvements
* Now it takes one click instead of two clicks to select a game in SelectGame window in 159289b
* Can't add games multiple times in 8e091ce
* If you have both Xenia versions you can choose which one the game will use

**Full Changelog**: https://github.com/xenia-manager/xenia-manager/compare/1.1.1...1.2.0

# [Version 1.1.1]

## What's Changed
* Fixed NVIDIA Framerate Limiter text not showing if the value was set to 0 in 0926090 (@shazzaam7)

**Full Changelog**: https://github.com/xenia-manager/xenia-manager/compare/1.1.0...1.1.1

# [Version 1.1.0]

## What's Changed
* Added settings for D3D12 and Vulkan by @shazzaam7 in https://github.com/xenia-manager/xenia-manager/pull/9
* Added some hacky settings for specific games that might need them by @shazzaam7 in  https://github.com/xenia-manager/xenia-manager/pull/9
* Add Input System Option by @shazzaam7 in https://github.com/xenia-manager/xenia-manager/pull/12
* Enable/Disable VSync through NVIDIA Drivers by @shazzaam7 in https://github.com/xenia-manager/xenia-manager/pull/13
* Added an icon by @shazzaam7 in https://github.com/xenia-manager/xenia-manager/pull/15
* Added NVIDIA Framerate Limiter option by @shazzaam7 in https://github.com/xenia-manager/xenia-manager/pull/16

**Full Changelog**: https://github.com/xenia-manager/xenia-manager/compare/1.0.0...1.1.0

# [Version 1.0.0]

Initial Release
