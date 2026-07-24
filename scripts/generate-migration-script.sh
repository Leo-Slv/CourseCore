#!/usr/bin/env bash
set -euo pipefail

OUTPUT="${1:-./artifacts/migrations/coursecore-migration.sql}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPOSITORY_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
OUTPUT_PATH="$REPOSITORY_ROOT/${OUTPUT#./}"
OUTPUT_DIR="$(dirname "$OUTPUT_PATH")"

mkdir -p "$OUTPUT_DIR"

cd "$REPOSITORY_ROOT"

dotnet ef migrations script \
  --context CourseCoreDbContext \
  --idempotent \
  --output "$OUTPUT_PATH"
