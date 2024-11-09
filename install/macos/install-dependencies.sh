#!/bin/bash

if [ ! -d "/opt/devkitpro/devkitARM" ]; then
    installer -pkg devkitpro-pacman-installer.pkg -target /
    xcode-select --install
    dkp-pacman -Sy
    dkp-pacman -S nds-dev
fi

if ! [ -x "$(command -v make)" ]; then
    if ! [ -x "$(command -v brew)" ]; then
        curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh
    fi
    brew install make
fi
