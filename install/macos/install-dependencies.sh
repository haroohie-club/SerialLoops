#!/bin/zsh

if [ ! -d "/opt/homebrew" ]; then
    NONINTERACTIVE=1 /bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)" 2>> /var/tmp/SerialLoopsInstaller.log
fi

if [ ! -d "/opt/homebrew/opt/llvm" ]; then
    /bin/zsh -c "brew install llvm" 2>> /var/tmp/SerialLoopsInstaller.log
    /bin/zsh -c "brew install lld" 2>> /var/tmp/SerialLoopsInstaller.log
fi

if [ ! -d "/opt/homebrew/bin/ninja" ]; then
   /bin/zsh -c "brew install ninja" 2>> /var/tmp/SerialLoopsInstaller.log
fi

exit 0
