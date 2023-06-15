<h1 align="center">
    <img alt="Serial Loops app icon; the letters 'SL' emblazoned above four translucent gray rings within a rounded square box colored with a blue-to-green gradient along the negative X-Z axis" src="src/SerialLoops/Icons/AppIcon.png" width="135px" />
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

**Serial Loops** is a fully-fledged editor for the Nintendo DS game, _Suzumiya Haruhi no Chokuretsu_ (The Series of Haruhi Suzumiya).

## Screenshots
<p align="center">
  <img width="325px" src="https://haroohie.club/images/chokuretsu/serial-loops/script-editor.png" alt="Screenshot of the Serial Loops script editor, featuring the 'EV2_029' script being edited. A list of commands is displayed in a list view panel, with buttons to add, remove and clear commands, with information about the currently selected command displayed on the right. Haruhi and Tsuruya are displayed on a preview of the Nintendo DS screen." />
  <img width="325px" src="https://haroohie.club/images/chokuretsu/serial-loops/map-editing.png" alt="Screenshot of the Serial Loops map editor, featuring the 'SLD1' map open with checkboxes to show/hide the camera position and collision grid" />
  <img width="325px" src="https://haroohie.club/images/chokuretsu/serial-loops/sound-editing.png" alt="Screenshot of the Serial Loops sound editor, featuring a modal widget with a sound wave graph. Buttons to start and stop playback are present, as are sliders and a checkbox to enable looping and adjust the track loop start and end points." />
  <img width="325px" src="https://haroohie.club/images/chokuretsu/serial-loops/home-screen.png" alt="Screenshot of the Serial Loops home screen. The Serial Loops logo and title sits at the top of the menu. Below that, under 'Start' on the left hand side, options to create a project, open an existing project, and modify preferences are present. An empty list of 'Recents' is visible on the right hand side, where recent projects would appear." />
</p>

## Installation
### Prerequisites
#### Installing devKitARM
[devkitARM](https://devkitpro.org/wiki/Getting_Started) is required to use Serial Loops on all platforms.

* Using the Windows graphical installer, you can simply select the devkitARM (Nintendo DS) workloads
* On macOS and Linux, run `sudo dkp-pacman -S nds-dev` from the terminal after installing the devkitPro pacman distribution.

#### Installing OpenAL (Linux)
If you're running on Linux and _not using one of the packaged releases_, you will also need to install OpenAL, needed for audio processing. On Ubuntu/Debian (which are the distros we test on),
it can be installed in a single command:
```bash
sudo apt install libopenal-dev
```

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

### Building
Serial Loops requires the .NET 6.0 SDK to build. You can download it [here](https://dotnet.microsoft.com/download/dotnet/6.0). To build Serial Loops for your platform, run:

```bash
dotnet build src/PLATFORM
```

Remember to replace `PLATFORM` with the platform you're on:
* `SerialLoops.Gtk` for Linux
* `SerialLoops.Mac` for macOS
* `SerialLoops.Wpf` for Windows

We recommend Visual Studio 2022 for development. If you'd like to contribute new features or fixes, we recommend [getting in touch on Discord first](https://discord.gg/nesRSbpeFM) before submitting a Pull Request!