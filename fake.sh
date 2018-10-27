#!/usr/bin/env bash
set -euo pipefail

./paket.sh restore --fail-on-checks

./packages/build/FAKE/tools/FAKE.exe build.fsx $@
