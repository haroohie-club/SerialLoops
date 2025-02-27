#!/bin/bash

COPYTO=$1

git clone https://github.com/PortAudio/portaudio.git
pushd portaudio
cmake . -G "Unix Makefiles" -DCMAKE_INSTALL_PREFIX=./build/lib -DCMAKE_POLICY_VERSION_MINIMUM=3.5 -DPA_BUILD_SHARED_LIBS=true
make
if [ $COPYTO ]; then
    cp -L ./libportaudio.dylib $COPYTO
fi
popd
rm -rf portaudio