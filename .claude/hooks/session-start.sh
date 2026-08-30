#!/bin/bash
set -euo pipefail

# Only needed in Claude Code on the web — a local dev machine already has the SDK.
if [ "${CLAUDE_CODE_REMOTE:-}" != "true" ]; then
  exit 0
fi

if command -v dotnet >/dev/null 2>&1; then
  exit 0
fi

sudo apt-get update -qq
sudo apt-get install -y -qq dotnet-sdk-8.0
