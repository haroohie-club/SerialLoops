Name:           SerialLoops
Version:        #VERSION#
Release:        1%{?dist}
Summary:        Editor for Suzumiya Haruhi no Chokuretsu
ExclusiveArch:  x86_64
License:        GPLv3
URL:            https://haroohie.club/chokurestu/serial-loops/
Source0:        %{name}-%{version}.tar.gz
Source1:        https://github.com/haroohie-club/SerialLoops
BuildRequires:  dotnet-sdk-8.0
Requires:       openal-soft make

%global debug_package %{nil}
%define __os_install_post %{nil}

%description
An editor for the Nintendo DS game Suzumiya Haruhi no Chokuretsu (The Series of Haruhi Suzumiya)

%prep
%setup -q

%build
dotnet publish src/SerialLoops/SerialLoops.csproj -c Release -f net8.0 -r linux-x64 --self-contained /p:DebugType=None /p:DebugSymbols=false /p:PublishSingleFile=true

%install
rm -rf %{buildroot}
mkdir -p %{buildroot}/%{_bindir}
mkdir -p %{buildroot}/%{_datadir}/applications
mkdir -p %{buildroot}/%{_libdir}/SerialLoops
cp -r src/SerialLoops/bin/Release/net8.0/linux-x64/publish/* %{buildroot}/%{_libdir}/SerialLoops/
chmod 777 %{buildroot}/%{_libdir}/SerialLoops/
ln -s %{_libdir}/SerialLoops/SerialLoops %{buildroot}/%{_bindir}/SerialLoops
printf "[Desktop Entry]\nVersion=%{version}\nName=Serial Loops\nComment=Editor for Suzumiya Haruhi no Chokuretsu\nExec=%{_bindir}/SerialLoops\nIcon=%{_libdir}/SerialLoops/Assets/Icons/AppIcon.png\nTerminal=false\nType=Application\nCategories=Utility;Application\n" > %{buildroot}/%{_datadir}/applications/SerialLoops.desktop
chmod +x %{buildroot}/%{_datadir}/applications/SerialLoops.desktop

%files
%{_bindir}/SerialLoops
%{_libdir}/SerialLoops
%{_datadir}/applications/SerialLoops.desktop
