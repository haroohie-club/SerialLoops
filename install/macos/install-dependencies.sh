#!/bin/bash

if [ ! -d "/opt/devkitpro/devkitARM" ]; then
    installer -pkg devkitpro-pacman-installer.pkg -target / -dumplog >> /var/tmp/SerialLoopsInstaller.log
    xcode-select --install || true >> /var/tmp/SerialLoopsInstaller.log
    dkp-pacman -Sy --noconfirm >> /var/tmp/SerialLoopsInstaller.log
    dkp-pacman -S --noconfirm nds-dev >> /var/tmp/SerialLoopsInstaller.log
fi

exit 0
