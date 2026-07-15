namespace Dynamis.UI.Windows;

partial class ChangelogWindow
{
    private void Draw0_1_3_14()
    {
        if (!DrawVersionHeader(0, 1, 3, 14, 0)) {
            return;
        }

        ImGuiComponents.SeparatorText("New features"u8);

        BulletText("Added a RSV/RSF viewer, accessible through /dynamis rsv."u8);
    }

    private void Draw0_1_3_13()
    {
        if (!DrawVersionHeader(0, 1, 3, 13, 0)) {
            return;
        }

        ImGuiComponents.SeparatorText("New features"u8);

        BulletText("Added a button to save Texture objects to TEX or DDS files."u8);
    }

    private void Draw0_1_3_12()
    {
        if (!DrawVersionHeader(0, 1, 3, 12, 0)) {
            return;
        }

        ImGuiComponents.SeparatorText("Miscellaneous"u8);

        BulletText("Updated for Dalamud 15."u8);
    }

    private void Draw0_1_3_11()
    {
        if (!DrawVersionHeader(0, 1, 3, 11, 0)) {
            return;
        }

        ImGuiComponents.SeparatorText("Miscellaneous"u8);

        BulletText("Updated for Dalamud 14."u8);
        BulletText("Updated to .NET 10."u8);
    }

    private void Draw0_1_3_10()
    {
        if (!DrawVersionHeader(0, 1, 3, 10, 0)) {
            return;
        }

        ImGuiComponents.SeparatorText("Bug fixes and improvements"u8);

        BulletText("Made data.yml parsing more robust against duplicates."u8);
    }

    private void Draw0_1_3_9()
    {
        if (!DrawVersionHeader(0, 1, 3, 9, 0)) {
            return;
        }

        ImGuiComponents.SeparatorText("Bug fixes and improvements"u8);

        BulletText("Fixed an out-of-bounds error in the hex viewers in annotated mode."u8);
    }

    private void Draw0_1_3_8()
    {
        if (!DrawVersionHeader(0, 1, 3, 8, 0)) {
            return;
        }

        ImGuiComponents.SeparatorText("Bug fixes and improvements"u8);

        BulletText("Changed some clickable pointers to be right-aligned."u8);
    }

    private void Draw0_1_3_7()
    {
        if (!DrawVersionHeader(0, 1, 3, 7, 0)) {
            return;
        }

        ImGuiComponents.SeparatorText("IPC API"u8);

        BulletText("Added ImGuiDrawPointer v3 and equivalent Get...Delegate IPC functions."u8);
        BulletText("The IPC API version is now 1.6."u8);
    }

    private void Draw0_1_3_6()
    {
        if (!DrawVersionHeader(0, 1, 3, 6, 0)) {
            return;
        }

        ImGuiComponents.SeparatorText("Bug fixes and improvements"u8);

        BulletText("Started allowing a 0x prefix in front of the address in the /dynamis inspect command."u8);
        BulletText("Fixed an issue with generic type resolution."u8);
    }

    private void Draw0_1_3_5()
    {
        if (!DrawVersionHeader(0, 1, 3, 5, 0)) {
            return;
        }

        ImGuiComponents.SeparatorText("New features"u8);

        BulletText("Added a setting to silence data.yml-related errors."u8);
    }

    private void Draw0_1_3_3()
    {
        if (!DrawVersionHeader(0, 1, 3, 3, 0)) {
            return;
        }

        ImGuiComponents.SeparatorText("Miscellaneous"u8);

        BulletText("Updated for Dalamud 13."u8);
    }

    private void Draw0_1_3_2()
    {
        if (!DrawVersionHeader(0, 1, 3, 2, 0)) {
            return;
        }

        ImGuiComponents.SeparatorText("IPC API"u8);

        BulletText("Added ImGuiDrawPointer v2 and equivalent Get...Delegate IPC functions."u8);
        BulletText("The IPC API version is now 1.5."u8);
    }

    private void Draw0_1_3_1()
    {
        if (!DrawVersionHeader(0, 1, 3, 1, 0)) {
            return;
        }

        ImGuiComponents.SeparatorText("Bug fixes and improvements"u8);

        BulletText("Fixed a bug with generic type parsing."u8);
        BulletText("Improved handling of unsupported type in the pointer thunk generator."u8);
    }

    private void Draw0_1_3_0()
    {
        if (!DrawVersionHeader(0, 1, 3, 0, 0)) {
            return;
        }

        ImGuiComponents.SeparatorText("New features"u8);

        BulletText("Added support for generic types from ClientStructs."u8);
        BulletText("Added an option to pass a name to the Show-Object cmdlet."u8);

        ImGuiComponents.SeparatorText("IPC API"u8);

        BulletText("Added InspectObject v2 and InspectRegion v2 IPC functions."u8);
        BulletText("The IPC API version is now 1.4."u8);

        ImGuiComponents.SeparatorText("Bug fixes and improvements"u8);

        BulletText("Improved handling of class pre-identification."u8);
        BulletText("Improved IPC thread safety."u8);
        BulletText("Improved some wording in the Object Inspector."u8);
        BulletText(
            "Prevented double-initialization of the Symbol Handler in case of plugin restart (causing lag spikes)."u8
        );
        BulletText("Improved the function disassembler's end detection logic."u8);
        BulletText("Fixed more IPFD initialization issues."u8);
        BulletText("Fixed parsing of some information from ClientStructs' data.yml."u8);
        BulletText("Improved handling of nested fields in the Object Inspector."u8);
        BulletText("Improved hex viewer annotated mode."u8);
    }

    private void Draw0_1_2_1()
    {
        if (!DrawVersionHeader(0, 1, 2, 1, 0)) {
            return;
        }

        ImGuiComponents.SeparatorText("Bug fixes and improvements"u8);

        BulletText(
            "Handled ref types in the generated pointer thunks supporting the Object Inspector and the hosted PowerShell."u8
        );
        BulletText("Fixed a crash related to IPFD initialization."u8);
    }

    private void Draw0_1_2_0()
    {
        if (!DrawVersionHeader(0, 1, 2, 0, 0)) {
            return;
        }

        ImGuiComponents.SeparatorText("New features"u8);

        BulletText("Revamped the Settings window."u8);

        ImGuiComponents.SeparatorText("Bug fixes and improvements"u8);

        BulletText("Added new interface settings: Serious mode, and automatic annotated mode for hex viewers."u8);
        BulletText("Improved the class identification logic."u8);
    }

    private void Draw0_1_1_0()
    {
        if (!DrawVersionHeader(0, 1, 1, 0, 0)) {
            return;
        }

        ImGuiComponents.SeparatorText("New features"u8);

        BulletText(
            "Hex viewers now have an annotated mode, with 8 bytes per row followed by the fields they contain."u8
        );
        BulletText(
            "Array size is now taken into account for pointer fields that have a similarly-named, span-typed property."u8
        );
        BulletText(
            "IPFD breakpoints can now be configured to only keep a single thread snapshot per instruction address and/or type of the \"this\" argument."u8
        );

        ImGuiComponents.SeparatorText("Bug fixes and improvements"u8);

        BulletText(
            "Made some tooltips that explain why buttons are disabled only appear when they actually are disabled."u8
        );
        BulletText(
            "Moved the PowerShell layer on the plugin icon to prevent it from being occluded by Dalamud's icon overlays."u8
        );

        ImGuiComponents.SeparatorText("Miscellaneous"u8);

        BulletText("Added documentation in the GitHub repository."u8);
    }

    private void Draw0_1_0_0()
    {
        if (!DrawVersionHeader(0, 1, 0, 0, 0)) {
            return;
        }

        ImGuiComponents.SeparatorText("New features"u8);

        BulletText("Added a hosted PowerShell."u8);
        BulletText(
            "Two \"editions\" of the plugin are now distributed through the repository. One includes everything, the other excludes the hosted PowerShell at compile time."u8
        );
        BulletText(
            "Various cmdlets, ad hoc types and other infrastructure are provided to the hosted PowerShell, to facilitate interacting with native objects and Dalamud services."u8
        );
        BulletText(
            "The Symbol Handler can now be turned off on Windows as well, and also has a Force Initialize mode, equivalent to how it operates on Wine."u8
        );

        ImGuiComponents.SeparatorText("IPC API"u8);

        BulletText("Added GetApiVersion IPC function."u8);
        BulletText("Added ApiInitialized and ApiDisposed IPC events."u8);
        BulletText(
            "The IPC API version is now 1.3. (Previous versions could not be told apart except through feature detection.)"u8
        );

        ImGuiComponents.SeparatorText("Bug fixes and improvements"u8);

        BulletText("Made the Resource Handle inspector more robust."u8);
    }

    private void Draw0_0_1_15()
    {
        if (!DrawVersionHeader(0, 0, 1, 15, 0)) {
            return;
        }

        ImGuiComponents.SeparatorText("Bug fixes and improvements"u8);

        BulletText("Fixed an address miscalculation related to EXE ASLR."u8);
    }

    private void Draw0_0_1_14()
    {
        if (!DrawVersionHeader(0, 0, 1, 14, 0)) {
            return;
        }

        ImGuiComponents.SeparatorText("IPC API"u8);

        BulletText("Added GetClass, IsInstanceOf and PreloadDataYaml IPC functions."u8);

        ImGuiComponents.SeparatorText("Bug fixes and improvements"u8);

        BulletText("Added various \"Copy to clipboard\" buttons."u8);
    }

    private void Draw0_0_1_13()
    {
        if (!DrawVersionHeader(0, 0, 1, 13, 0)) {
            return;
        }

        ImGuiComponents.SeparatorText("New features"u8);

        BulletText("The Toolbox and Settings windows now display Dynamis's version."u8);
        BulletText("The Symbol Handler can now be turned off on Wine."u8);
    }

    private void Draw0_0_1_12()
    {
        if (!DrawVersionHeader(0, 0, 1, 12, 0)) {
            return;
        }

        ImGuiComponents.SeparatorText("Bug fixes and improvements"u8);

        BulletText("EXE ASLR is now actually handled (since 0.0.1.11)."u8);
        BulletText("Fixed various crashes related to EXE ASLR."u8);
        BulletText("Fixed other Object Inspector bugs."u8);
    }

    private void Draw0_0_1_5()
    {
        if (!DrawVersionHeader(0, 0, 1, 5, 0)) {
            return;
        }

        ImGuiComponents.SeparatorText("New features"u8);

        BulletText(
            "Thread snapshots from IPFD breakpoints now have stack frame information, and their stacks are organized by frame, when possible."u8
        );
        BulletText("Basic information is now displayed about objects from libraries (such as Direct3D and DXGI)."u8);
        BulletText(
            "ClientStructs' data.yml can now be fetched from the project's GitHub repository and managed automatically."u8
        );

        ImGuiComponents.SeparatorText("Bug fixes and improvements"u8);

        BulletText("Improved the function disassembler's end detection logic."u8);
        BulletText("Fixed a bug when hovering snapshots."u8);

        ImGuiComponents.SeparatorText("Miscellaneous"u8);

        BulletText("Updated for Dalamud 12."u8);
        BulletText("Updated to .NET 9."u8);
    }

    private void Draw0_0_1_4()
    {
        if (!DrawVersionHeader(0, 0, 1, 4, 0)) {
            return;
        }

        ImGuiComponents.SeparatorText("New features"u8);

        BulletText("The Object Inspector can now process multiple inheritance / interface pointers."u8);
        BulletText(
            "Symbols can now be resolved (when supported by the underlying platform): addresses can now be turned into the form foo.exe!DoTheThing+0x1234."u8
        );
    }

    private void Draw0_0_1_3()
    {
        if (!DrawVersionHeader(0, 0, 1, 3, 0)) {
            return;
        }

        ImGuiComponents.SeparatorText("Initial release"u8);

        BulletText("Previous versions had to be built from source."u8);
        BulletText("This is the first release distributed through a repository."u8);
    }
}
