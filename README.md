Serial Loops is a level editor for the Nintendo DS game Suzumiya Haruhi no Chokuretsu (The Series of Haruhi Suzumiya).

## Installation
The following prerequisites need to be installed in order to use Serial Loops:

* The [.NET 6.0 runtime](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)
* [devkitARM](https://devkitpro.org/wiki/Getting_Started).
    - Using the Windows graphical installer, you can simply select the devkitARM (Nintendo DS) workloads
    - On macOS and Linux, run `sudo dkp-pacman -S nds-dev` from the terminal after installing the devkitPro pacman distribution.

Additionally, on Linux, you will need to install OpenAL. On Ubuntu/Debian (which are the distros we test on), it can be installed in a single command:
```
sudo apt install libopenal-dev
```

## Bugs
Please file bugs in the Issues tab in this repository. Please include the following information:
* The platform you are running Serial Loops on
* The version of the Chokuretsu ROM you are using (Japanese, patched English ROM, etc.)
* A description of the steps required to reproduce the issue
* The relevant logs for the issue (can be found in ~/SerialLoops/Logs)