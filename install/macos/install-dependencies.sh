#!/bin/bash

if [ ! -d "/opt/devkitpro/devkitARM" ]; then
    installer -pkg devkitpro-pacman-installer.pkg -target /
    xcode-select --install
    dkp-pacman -Sy
    dkp-pacman -S nds-dev
fi
