#!/bin/bash

if [ ! -d "/opt/devkitpro/devkitARM" ]; then
    installer -pkg devkitpro-pacman-installer.pkg -target / -dumplog
    xcode-select --install
    dkp-pacman -Sy
    dkp-pacman -S --noconfirm nds-dev
fi
