#! /bin/sh
set -e

./paket.sh restore --fail-on-checks

./packages/FAKE/tools/FAKE.exe build.fsx $@
