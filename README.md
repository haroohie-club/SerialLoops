<h1 align="center">
    <img alt="Serial Loops app icon; the letters 'SL' emblazoned above four translucent gray rings within a rounded square box colored with a blue-to-green gradient along the negative X-Z axis" src="src/SerialLoops/Assets/Icons/AppIcon.png" width="135px" />
    <br/>
    Serial Loops
</h1>
<p align="center">
    <a href="https://discord.gg/nesRSbpeFM">
        <img alt="Haroohie Translation Club Discord Server badge " src="https://img.shields.io/discord/904791358609424436.svg?label=&logo=discord&logoColor=fff&color=7389D8&labelColor=6A7EC2" />
    </a>
    <a href="https://haroohie.club/chokuretsu/serial-loops/docs">
        <img alt="Serial Loops documentation link badge" src="https://img.shields.io/badge/docs-haroohie.club-00C4F5?logo=github" />
    </a>
</p>
<p align="center">
    <a href='https://flathub.org/apps/club.haroohie.SerialLoops'>
        <img width='140' alt='Get it on Flathub' src='https://flathub.org/api/badge?locale=en'/>
    </a>
</p>

**Serial Loops** is a full-fledged editor for the Nintendo DS game _Suzumiya Haruhi no Chokuretsu_ (The Series of Haruhi Suzumiya).

## Screenshots
<p align="center">
  <img width="325px" src="https://haroohie.club/images/chokuretsu/serial-loops/script-editor.png" alt="Screenshot of the Serial Loops script editor, featuring the 'EV2_029' script being edited. A list of commands is displayed in a list view panel, with buttons to add, remove and clear commands, with information about the currently selected command displayed on the right. Haruhi and Tsuruya are displayed on a preview of the Nintendo DS screen." />
  <img width="325px" src="https://haroohie.club/images/chokuretsu/serial-loops/map-editing.png" alt="Screenshot of the Serial Loops map editor, featuring the 'SLD1' map open with checkboxes to show/hide the camera position and collision grid" />
  <img width="325px" src="https://haroohie.club/images/chokuretsu/serial-loops/bgm-loop-editing.png" alt="Screenshot of the Serial Loops sound editor, featuring a modal widget with a sound wave graph. Buttons to start and stop playback are present, as are sliders and a checkbox to enable looping and adjust the track loop start and end points." />
  <img width="325px" src="https://haroohie.club/images/chokuretsu/serial-loops/home-screen.png" alt="Screenshot of the Serial Loops home screen. The Serial Loops logo and title sits at the top of the menu. Below that, under 'Start' on the left hand side, options to create a project, open an existing project, and modify preferences are present. An empty list of 'Recents' is visible on the right hand side, where recent projects would appear." />
</p>

## Authors
### Developers
Serial Loops is developed by:
* [Jonko](https://github.com/jonko0493) &ndash; Systems architect & reverse engineering work
* [William278](https://william278.net) &ndash; UX architect & design work

Additional contributions have been made by:
* Fuyuko Ayumu
* Xzonn

### Translators
Serial Loops is translated into a variety of langauges thanks to the following contributors:
* Chinese (Simplified/Traditional): [Xzonn](https://xzonn.top) (traditional is auto-generated via [OpenCC](https://github.com/BYVoid/OpenCC))
* Italian: Oropuro_49 and Fuyuko Ayumu
* Japanese: Amelia Chaplin

## Documentation
Documentation for how to use Serial Loops can be found on [our website](https://haroohie.club/chokuretsu/serial-loops/).

## Installation
### Prerequisites
It is recommended that you use a distribution of Serial Loops that automatically installs or comes with the necessary prerequisites. For each platform these are:

* Linux: [Flatpak from Flathub](https://flathub.org/apps/club.haroohie.SerialLoops)
  - The AppImage, deb, and rpm packages are also easy to use, but if you're not sure which to choose, go with the Flatpak
* macOS: Installer
* Windows: Installer

Using these will ensure Serial Loops is ready to use after installation. However, if you would rather use a portable build on Windows/Linux, please check the information on installing these prerequisites in the next section.

#### Dependencies for Non-Packages Releases
If you opt to use one of the non-packaged releases on Windows or Linux, you will need to install a few dependencies. These are:

* Clang and LLD from [LLVM](http://llvm.org) (on Windows, it's best to just use the LLVM installer as it will install both of these; on Linux, you can opt to install just the `clang`, `lld`, and possibly `llvm` packages from your package manager)
* [Ninja](https://ninja-build.org)
* On Linux, you will also need SDL2

#### A Nintendo DS Emulator
To test the game easily, you will want to have a Nintendo DS emulator installed. We recommend using [melonDS](https://melonds.kuribo64.net/) for its accuracy. If you are using the Flatpak release, melonDS comes pre-packaged with it.

### Download & Install
Once you have installed any necessary prerequisites, to install Serial Loops, download the latest release for your platform from the [Releases tab](https://github.com/haroohie-club/SerialLoops/releases).

Be sure to [read the Serial Loops documentation](https://haroohie.club/chokuretsu/serial-loops/docs) for instructions on how to use it!

### Uninstalling
Uninstalling Serial Loops itself is quite simple; however, you may also want to uninstall the packaged dependencies. Follow the instructions below for each platform to do this.

#### Linux
* If you installed the Flatpak, simply run uninstall it via the Warehouse app or your software/app store application. Alternatively, you can run `flatpak uninstall --delete-data club.haroohie.SerialLoops` to remove the Flatpak and all its associated data. If you don't want to keep your project data, ensure you delete the `~/SerialLoops` directory as well.
* If you installed the AppImage, simply delete it from your machine.
* If you installed the deb or rpm package, run your package manager's uninstall command for the `SerialLoops` package.
* If you downloaded the tarball, delete the unpacked files from your system. You may also run your package manager's uninstallation command for Clang, LLD, Ninja, and SDL2.

#### macOS
Simply drag the Serial Loops application from the Applications folder to the trash. Then run `brew uninstall llvm` and `brew uninstall ninja-build` to remove LLVM and Ninja (if you want to do so). Finally, if you also want to uninstall Homebrew, you can run `/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/uninstall.sh)"`.

#### Windows
Go into Add or Remove Programs and remove Serial Loops and (if you don't want it anymore) LLVM.

## Bugs
Please file bugs in the Issues tab in this repository. Please include the following information:
* The platform you are running Serial Loops on
* The version of the _Chokuretsu_ ROM you are using (Japanese, patched English ROM, etc.)
* A description of the steps required to reproduce the issue
* The relevant logs for the issue (can be found in ~/SerialLoops/Logs)

## Development
### License
Serial Loops is licensed under the GPLv3. See [LICENSE](LICENSE) for more information.

### Prerequisites
Serial Loops requires the .NET 8.0 SDK to build. You can download it [here](https://dotnet.microsoft.com/download/dotnet/8.0).

Additionally, on macOS, you will have to install CMake so that the build can compile SDL2. To do this, download the macOS dmg for your
[here](https://cmake.org/download/) and install it. Then run:
```bash
sudo /Applications/CMake.app/Contents/bin/cmake-gui --install
```
This will symlink the CMake binaries to `/usr/local/bin` which is necessary for the build to work. Alternatively, you can install
cmake via Homebrew with `brew install cmake`.

On Linux, you will need to install the SDL2 binaries for your distribution.

### Building
To build Serial Loops for your platform, run:
```bash
dotnet build
```
On Linux/Mac, you need to specify the target framework:
```bash
dotnet build -f net8.0
```
Specifying this prevents dotnet from trying to build the Windows project, which can cause errors.

We recommend [Rider](https://www.jetbrains.com/rider/) for development as it has the best Avalonia support and is now free to use for non-commercial purposes; however, on Windows, you can also use [Visual Studio 2022](https://visualstudio.microsoft.com/).
You can also build from both of these IDEs; however, when building from Rider on Linux/Mac, you must go into **Settings &rarr; Build, Execution, Deployment &rarr; Toolset and Build** and add `TargetFramework=net8.0`
to the MSBuild global properties field. This has the same effect as specifying `-f net8.0` on the command line.

If you'd like to contribute new features or fixes, we recommend [getting in touch on Discord first](https://discord.gg/nesRSbpeFM) before submitting a pull request!

### Testing
Serial Loops has headless tests that run to test the UI and other functionality of the program.

Tests can be run via `dotnet test` (make sure to add `-f net8.0` on Linux or Mac) or through the test runners in Rider or Visual Studio.
