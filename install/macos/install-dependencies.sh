#!/bin/bash

if [ ! -d "/opt/devkitpro/devkitARM" ]; then
    installer -pkg devkitpro-pacman-installer.pkg -target / -dumplog >> /var/tmp/SerialLoopsInstaller.log
    xcode-select --install || echo "xcode tools already installed" >> /var/tmp/SerialLoopsInstaller.log
    dkp-pacman -Sy --noconfirm >> /var/tmp/SerialLoopsInstaller.log
    dkp-pacman -S --noconfirm nds-dev >> /var/tmp/SerialLoopsInstaller.log
fi
