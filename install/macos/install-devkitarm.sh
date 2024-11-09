#!/bin/bash

if [ ! -d "/opt/devkitpro/devkitARM" ]; then
    dkp-pacman -Sy --noconfirm
    dkp-pacman -S --noconfirm nds-dev
fi
