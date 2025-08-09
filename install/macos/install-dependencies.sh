#!/bin/zsh

if [ ! -d "/opt/homebrew" ]; then
    NONINTERACTIVE=1 /bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)" 2>> /var/tmp/SerialLoopsInstaller.log
fi

if [ ! -d "/opt/homebrew/opt/llvm" ]; then
    /opt/homebrew/bin/brew install llvm 2>> /var/tmp/SerialLoopsInstaller.log
    /opt/homebrew/bin/brew install lld 2>> /var/tmp/SerialLoopsInstaller.log
fi

if [ ! -d "/opt/homebrew/bin/ninja" ]; then
    /opt/homebrew/bin/brew install ninja 2>> /var/tmp/SerialLoopsInstaller.log
fi

exit 0
