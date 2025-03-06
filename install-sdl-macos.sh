#!/bin/bash

COPYTO=$1

git clone -b release-2.32.x https://github.com/libsdl-org/SDL.git
pushd SDL
cmake -S . -B build -DSDL_SHARED=ON -DCMAKE_SYSTEM_NAME=Darwin -DCMAKE_OSX_ARCHITECTURES="arm64;x86_64"
cmake --build build
if [ $COPYTO ]; then
    cp -L ./build/libSDL2-2.0.dylib $COPYTO
fi
popd
rm -rf SDL