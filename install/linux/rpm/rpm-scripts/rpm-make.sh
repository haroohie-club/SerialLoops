#!/bin/bash

dnf install -y rpmdevtools rpmlint dotnet-sdk-8.0
rpmbuild -bb /root/rpmbuild/SPECS/SerialLoops.spec
