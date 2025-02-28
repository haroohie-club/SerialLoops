<h1 align="center">
    <img alt="Serial Loops app icon; the letters 'SL' emblazoned above four translucent gray rings within a rounded square box colored with a blue-to-green gradient along the negative X-Z axis" src="src/SerialLoops/Assets/Icons/AppIcon.png" width="135px" />
    <br/>
    Serial Loops
</h1>
<p align="center">
    <a href="https://dev.azure.com/jonko0493/haroohie-private/_apis/build/status%2FSerialLoops-Official?branchName=main">
        <img alt="Azure Pipelines build status badge" src="https://dev.azure.com/jonko0493/haroohie-private/_apis/build/status%2FSerialLoops-Official?branchName=main" />
    </a>
    <a href="https://discord.gg/nesRSbpeFM">
        <img alt="Haroohie Translation Club Discord Server badge " src="https://img.shields.io/discord/904791358609424436.svg?label=&logo=discord&logoColor=fff&color=7389D8&labelColor=6A7EC2" />
    </a>
    <a href="https://haroohie.club/chokuretsu/serial-loops/docs">
        <img alt="Serial Loops documentation link badge" src="https://img.shields.io/badge/docs-haroohie.club-00C4F5?logo=github" />
    </a>
</p>

**Serial Loops** is a full-fledged editor for the Nintendo DS game _Suzumiya Haruhi no Chokuretsu_ (The Series of Haruhi Suzumiya).

## Screenshots
<p align="center">
  <img width="325px" src="https://haroohie.club/images/chokuretsu/serial-loops/script-editor.png" alt="Screenshot of the Serial Loops script editor, featuring the 'EV2_029' script being edited. A list of commands is displayed in a list view panel, with buttons to add, remove and clear commands, with information about the currently selected command displayed on the right. Haruhi and Tsuruya are displayed on a preview of the Nintendo DS screen." />
  <img width="325px" src="https://haroohie.club/images/chokuretsu/serial-loops/map-editing.png" alt="Screenshot of the Serial Loops map editor, featuring the 'SLD1' map open with checkboxes to show/hide the camera position and collision grid" />
  <img width="325px" src="https://haroohie.club/images/chokuretsu/serial-loops/sound-editing.png" alt="Screenshot of the Serial Loops sound editor, featuring a modal widget with a sound wave graph. Buttons to start and stop playback are present, as are sliders and a checkbox to enable looping and adjust the track loop start and end points." />
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

* Linux: Flatpak
* macOS: Installer
* Windows: Installer

Using these will ensure Serial Loops is ready to use after installation. However, if you would rather use a portable build on Windows/Linux, please check the information on installing
these prerequisites below.

<details>
    <summary>View prerequisites for non-Flatpak/installer distributions</summary>

#### Installing devkitARM
[devkitARM](https://devkitpro.org/wiki/Getting_Started) is required to use Serial Loops on all platforms.

* Using the Windows graphical installer, you can simply select the devkitARM (Nintendo DS) workloads
* On macOS and Linux, run `sudo dkp-pacman -S nds-dev` from the terminal after installing the devkitPro pacman distribution.

#### Installing Make or Docker
To assemble ASM hacks you want to apply, you will need to decide whether to use Make or Docker. Make is automatically installed when using the Debian and RPM
packages we distribute, so you don't need to worry about this step if you're using either of those.

Currently, the Docker path is **only supported on Windows** due to operating system and framework limitations. It is possible to get Docker running
just fine on Linux distros by running SerialLoops as root (e.g. `sudo SerialLoops`), but it's easier to just use Make. On macOS, there is no known
way of getting the Docker path to work, so you will have to use Make.

* [Make](https://www.gnu.org/software/make/) is the software used to assemble assembly hacks. Installing Make allows you to build the hacks
  directly on your system.
    - To install on Windows, you will have to use a terminal and a package manager. Your options are Winget (installed by default on Win10+) or
      [Chocolatey](https://chocolatey.org/). Open an admin PowerShell or Terminal window (Winkey + X + A) and enter `winget install GnuWin32.make`
      for Winget or `choco install make` for Chocolatey. If using Winget, you will then have to go into system preferences and add Make to the path.
    - Installation on macOS can be done through Xcode or Homebrew. If using Xcode, open a terminal and type `xcode-select --install`. If you would
      rather use Homebrew, open a terminal after installing Homebrew and type `brew install make`.
    - Make comes preinstalled on many Linux distributions, and if you're using the Debian or RPM package, it was definitely installed when you installed
      Serial Loops. If you're using the tar.gz it is not installed on yours, you will likely be able to install it as simply as
      `[packagemanger] install make` from a terminal.

  To test if make is installed properly, type `make --verison` into a terminal and see if it produces the version of make.
* If you would rather not install Make, or if it is not working properly, you can instead run it through a Docker container. To do this, you should
  install [Docker Desktop](https://www.docker.com/products/docker-desktop/) or the Docker Engine. Ensure the Docker engine is running and make sure
  to check the "Use Docker for ASM Hacks" option in Preferences. You may want to occasionally clean up containers created by Serial Loops, as it will
  create many of them.
    - On Windows, you will additionally need to install [Windows Subsystem for Linux (WSL)](https://learn.microsoft.com/en-us/windows/wsl/install).
      From an admin PowerShell or Terminal window (Winkey + X + A), simply type `wsl --install` to install it.

#### Installing SDL2 (Linux)
If you're running on Linux and _not using one of the package releases_ (the Flatpak, AppImage, `.deb` or `.rpm`), you will also need to install SDL2 which is used for audio processing.

</details>

#### A Nintendo DS Emulator
To test the game easily, you will want to have a Nintendo DS emulator installed. We recommend using [melonDS](https://melonds.kuribo64.net/) for its accuracy.

### Download & Install
Once you have installed any necessary prerequisites, to install Serial Loops, download the latest release for your platform from the [Releases tab](https://github.com/haroohie-club/SerialLoops/releases).

Be sure to [read the Serial Loops documentation](https://haroohie.club/chokuretsu/serial-loops/docs) for instructions on how to use it!

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
This will symlink the CMake binaries to `/usr/local/bin` which is necessary for the build to work.

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
Serial Loops has headless tests that run to test the UI and other functionality of the program. To run tests locally, you will need to define either a `ui_vals.json` file or set an environment variable.

First, download [these test assets](https://haroohie.nyc3.cdn.digitaloceanspaces.com/bootstrap/serial-loops/test-assets.zip) -OutFile $(Build.ArtifactStagingDirectory)/test-assets.zip) and unzip them to a directory somewhere. Then, specify that directory in the `ui_vals.json` as `AssetsDirectory` or set the environment variable `ASSETS_DIRECTORY` to that path.

Tests can be run via `dotnet test` (make sure to add `-f net8.0` on Linux or Mac) or through the test runners in Rider or Visual Studio.
