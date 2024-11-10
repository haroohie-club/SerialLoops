from macos_pkg_builder import Packages
from sys import argv
import urllib.request

ver = argv[1]
rid = argv[2]

urllib.request.urlretrieve("https://github.com/devkitPro/pacman/releases/download/v6.0.2/devkitpro-pacman-installer.pkg", "devkitpro-pacman-installer.pkg")

with open("welcome.md") as welcome_file:
    welcome_md = welcome_file.read()
with open("../../README.md") as readme_file:
    readme_md = readme_file.read()
with open("../../LICENSE") as license_file:
    license_md = license_file.read()

pkg_obj = Packages(
    pkg_output=f"SerialLoops-{rid}.pkg",
    pkg_bundle_id="club.haroohie.serialloopsinstaller",
    pkg_file_structure={
        "./Serial Loops.app": "/Applications/Serial Loops.app",
    },
    pkg_preinstall_script="install-dependencies.sh",
    pkg_script_resources=['devkitpro-pacman-installer.pkg'],
    pkg_postinstall_script="cleanup-logs.sh",
    pkg_as_distribution=True,
    pkg_version=ver,
    pkg_welcome=welcome_md,
    pkg_readme=readme_md,
    pkg_license=license_md,
    pkg_title="Serial Loops"
)

assert pkg_obj.build() is True
