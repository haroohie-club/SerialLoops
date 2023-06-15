#!/bin/bash

dnf install -y rpmdevtools rpmlint dotnet-sdk-6.0
rpmbuild -bb /rpmbuild/SPECS/SerialLoops.spec