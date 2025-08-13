#!/bin/zsh --no-rcs

# This function is taken from the macOS-Pkg-Builder samples, licensed under the BSD-3-Clause license
# Copyright (c) 2023-2024, RIPEDA Consulting Corporation
# https://github.com/ripeda/macOS-Pkg-Builder
# https://github.com/ripeda/macOS-Pkg-Builder/blob/main/LICENSE.txt
_runAsUser() {
    local currentUser
    local uid

    currentUser=$( echo "show State:/Users/ConsoleUser" | /usr/sbin/scutil | /usr/bin/awk '/Name :/ { print $3 }' )
    uid=$(/usr/bin/id -u "${currentUser}")

	if [[ "${currentUser}" != "loginwindow" ]]; then
		/bin/launchctl asuser "$uid" sudo -u "${currentUser}" /usr/bin/open -a Terminal.app $@
	else
		echo "No user logged in, exiting..."
		exit 1
	fi
}

if [ ! -d "/opt/homebrew" ]; then
    NONINTERACTIVE=1 /bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)" 2>> /var/tmp/SerialLoopsInstaller.log
fi

if [ ! -d "/opt/homebrew/opt/llvm" ]; then
    echo "/opt/homebrew/bin/brew install llvm; exit 0" > /var/tmp/install_llvm.sh
    chmod +x /var/tmp/install_llvm.sh
    echo "/opt/homebrew/bin/brew install lld; exit 0" > /var/tmp/install_lld.sh
    chmod +x /var/tmp/install_lld.sh
    _runAsUser /var/tmp/install_llvm.sh 2>> /var/tmp/SerialLoopsInstaller.log
    _runAsUser /var/tmp/install_lld.sh 2>> /var/tmp/SerialLoopsInstaller.log
fi

if [ ! -d "/opt/homebrew/bin/ninja" ]; then
    echo "/opt/homebrew/bin/brew install ninja; exit 0" > /var/tmp/install_ninja.sh
    chmod +x /var/tmp/install_ninja.sh
    _runAsUser /var/tmp/install_ninja.sh 2>> /var/tmp/SerialLoopsInstaller.log
fi

exit 0
