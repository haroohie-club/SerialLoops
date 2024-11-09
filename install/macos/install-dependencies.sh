#!/bin/bash

if [ ! -d "/opt/devkitpro/devkitARM" ]; then
    installer -pkg devkitpro-pacman-installer.pkg -target /
    xcode-select --install || echo "xcode tools already installed"
    dkp-pacman -Sy --noconfirm
    dkp-pacman -S --noconfirm nds-dev
fi
