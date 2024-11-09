#!/bin/bash

if [ ! -d "/opt/devkitpro/devkitARM" ]; then
    sudo installer -pkg devkitpro-pacman-installer.pkg -target /
    sudo xcode-select --install
    sudo dkp-pacman -Sy
    sudo dkp-pacman -S nds-dev
fi
