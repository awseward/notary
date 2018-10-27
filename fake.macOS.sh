#!/usr/bin/env bash
set -euo pipefail

./paket.macOS.sh restore --fail-on-checks

mono ./packages/build/FAKE/tools/FAKE.exe build.fsx $@
