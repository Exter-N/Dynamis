# Version 0.1.4.0

## New features

- Added an item in Dalamud's dev menu.
- Improved the pointer/address inputs (ImGui fields and `/command` arguments) to accept `foo.exe+0x1234` and `sub_140001234` syntaxes.
- Added Direct3D and DXGI interface identification, and object description display where applicable.
- Added manual class specification to Object Inspector.
- Added a changelog. (You are currently reading it!)

## IPC API

- Added `InspectObject` v3, `ImGuiDrawPointer` v4 and equivalent `Get…Delegate` IPC functions.
- The IPC API version is now 1.7.

## Bug fixes and improvements

- Changed the UI colors to use Dalamud's new semantic colors.
- Sorted well-known objects by name in the Object Inspector's menu.
- Added a button to copy Resource Handle file names to clipboard.
- Added a right-click menu to ImGui pointer fields to copy the pointers to clipboard in various forms.
- Added the ability to specify a class manually in the `Get-ClientStruct` cmdlet.
- Exposed the RSV/RSF viewer in the menus.
- Truncated functions larger than 4 kiB in the disassembler to avoid performance issues.
- Improved string display in the annotated mode hex viewers.
- Improved handling of objects of unknown size.
- Improved support for pointer fields of type known to ClientStructs.

# Version 0.1.3.14

## New features

- Added a RSV/RSF viewer, accessible through `/dynamis rsv`.

# Version 0.1.3.13

## New features

- Added a button to save Texture objects to TEX or DDS files.

# Version 0.1.3.12

## Miscellaneous

- Updated for Dalamud 15.

# Version 0.1.3.11

## Miscellaneous

- Updated for Dalamud 14.
- Updated to .NET 10.

# Version 0.1.3.10

## Bug fixes and improvements

- Made data.yml parsing more robust against duplicates.

# Version 0.1.3.9

## Bug fixes and improvements

- Fixed an out-of-bounds error in the hex viewers in annotated mode.

# Version 0.1.3.8

## Bug fixes and improvements

- Changed some clickable pointers to be right-aligned.

# Version 0.1.3.7

## IPC API

- Added `ImGuiDrawPointer` v3 and equivalent `Get…Delegate` IPC functions.
- The IPC API version is now 1.6.

# Version 0.1.3.6

## Bug fixes and improvements

- Started allowing a 0x prefix in front of the address in the `/dynamis inspect` command.
- Fixed an issue with generic type resolution.

# Version 0.1.3.5

## New features

- Added a setting to silence data.yml-related errors.

# Version 0.1.3.3

## Miscellaneous

- Updated for Dalamud 13.

# Version 0.1.3.2

## IPC API

- Added `ImGuiDrawPointer` v2 and equivalent `Get…Delegate` IPC functions.
- The IPC API version is now 1.5.

# Version 0.1.3.1

## Bug fixes and improvements

- Fixed a bug with generic type parsing.
- Improved handling of unsupported type in the pointer thunk generator.

# Version 0.1.3.0

## New features

- Added support for generic types from ClientStructs.
- Added an option to pass a name to the `Show-Object` cmdlet.

## IPC API

- Added `InspectObject` v2 and `InspectRegion` v2 IPC functions.
- The IPC API version is now 1.4.

## Bug fixes and improvements

- Improved handling of class pre-identification.
- Improved IPC thread safety.
- Improved some wording in the Object Inspector.
- Prevented double-initialization of the Symbol Handler in case of plugin restart (causing lag spikes).
- Improved the function disassembler's end detection logic.
- Fixed more IPFD initialization issues.
- Fixed parsing of some information from ClientStructs' data.yml.
- Improved handling of nested fields in the Object Inspector.
- Improved hex viewer annotated mode.

# Version 0.1.2.1

## Bug fixes and improvements

- Handled ref types in the generated pointer thunks supporting the Object Inspector and the hosted PowerShell.
- Fixed a crash related to IPFD initialization.

# Version 0.1.2.0

## New features

- Revamped the Settings window.

## Bug fixes and improvements

- Added new interface settings: Serious mode, and automatic annotated mode for hex viewers.
- Improved the class identification logic.

# Version 0.1.1.0

## New features

- Hex viewers now have an annotated mode, with 8 bytes per row followed by the fields they contain.
- Array size is now taken into account for pointer fields that have a similarly-named, span-typed property.
- IPFD breakpoints can now be configured to only keep a single thread snapshot per instruction address and/or type of the `this` argument.

## Bug fixes and improvements

- Made some tooltips that explain why buttons are disabled only appear when they actually are disabled.
- Moved the PowerShell layer on the plugin icon to prevent it from being occluded by Dalamud's icon overlays.

## Miscellaneous

- Added documentation in the GitHub repository.

# Version 0.1.0.0

## New features

- Added a hosted PowerShell.
- Two "editions" of the plugin are now distributed through the repository. One includes everything, the other excludes the hosted PowerShell at compile time.
- Various cmdlets, ad hoc types and other infrastructure are provided to the hosted PowerShell, to facilitate interacting with native objects and Dalamud services.
- The Symbol Handler can now be turned off on Windows as well, and also has a Force Initialize mode, equivalent to how it operates on Wine.

## IPC API

- Added `GetApiVersion` IPC function.
- Added `ApiInitialized` and `ApiDisposed` IPC events.
- The IPC API version is now 1.3. (Previous versions could not be told apart except through feature detection.)

## Bug fixes and improvements

- Made the Resource Handle inspector more robust.

# Version 0.0.1.15

## Bug fixes and improvements

- Fixed an address miscalculation related to EXE ASLR.

# Version 0.0.1.14

## IPC API

- Added `GetClass`, `IsInstanceOf` and `PreloadDataYaml` IPC functions.

## Bug fixes and improvements

- Added various "Copy to clipboard" buttons.

# Version 0.0.1.13

## New features

- The Toolbox and Settings windows now display Dynamis's version.
- The Symbol Handler can now be turned off on Wine.

# Version 0.0.1.12

## Bug fixes and improvements

- EXE ASLR is now actually handled (since 0.0.1.11).
- Fixed various crashes related to EXE ASLR.
- Fixed other Object Inspector bugs.

# Version 0.0.1.5

## New features

- Thread snapshots from IPFD breakpoints now have stack frame information, and their stacks are organized by frame, when possible.
- Basic information is now displayed about objects from libraries (such as Direct3D and DXGI).
- ClientStructs' data.yml can now be fetched from the project's GitHub repository and managed automatically.

## Bug fixes and improvements

- Improved the function disassembler's end detection logic.
- Fixed a bug when hovering snapshots.

## Miscellaneous

- Updated for Dalamud 12.
- Updated to .NET 9.

# Version 0.0.1.4

## New features

- The Object Inspector can now process multiple inheritance / interface pointers.
- Symbols can now be resolved (when supported by the underlying platform): addresses can now be turned into the form foo.exe!DoTheThing+0x1234.

# Version 0.0.1.3

## Initial release

- Previous versions had to be built from source.
- This is the first release distributed through a repository.
