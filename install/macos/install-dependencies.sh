#!/bin/zsh

if [ ! -d "/opt/devkitpro/devkitARM" ]; then
    installer -pkg devkitpro-pacman-installer.pkg -target / -dumplog >> /var/tmp/SerialLoopsInstaller.log
    wait
    dkp-pacman -Sy --noconfirm 2>> /var/tmp/SerialLoopsInstaller.log
    dkp-pacman -S --noconfirm nds-dev 2>> /var/tmp/SerialLoopsInstaller.log
    xcode-select --install || true 2>> /var/tmp/SerialLoopsInstaller.log
fi

exit 0
