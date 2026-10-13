#!/usr/bin/env bash
# Runs ON the EC2 host (the CD workflow pipes it over SSH):
#
#   ssh user@host 'bash -s' -- ghcr.io/<owner>/coursecore:<tag> < deploy/aws/deploy.sh
#
# Pulls the image, restarts the stack, waits for the API to be healthy and, if it
# does not become healthy, puts the previously running image back.
set -euo pipefail

NEW_IMAGE="${1:?usage: deploy.sh <image-ref>}"
APP_DIR="${APP_DIR:-/opt/coursecore}"
HEALTH_TIMEOUT_SECONDS="${HEALTH_TIMEOUT_SECONDS:-120}"

cd "$APP_DIR"

PREVIOUS_IMAGE="$(docker inspect --format '{{.Config.Image}}' coursecore-api 2>/dev/null || true)"
echo "previous image: ${PREVIOUS_IMAGE:-<none>}"
echo "new image:      $NEW_IMAGE"

wait_until_healthy() {
  local deadline=$((SECONDS + HEALTH_TIMEOUT_SECONDS))
  while [ "$SECONDS" -lt "$deadline" ]; do
    local status
    status="$(docker inspect --format '{{if .State.Health}}{{.State.Health.Status}}{{else}}none{{end}}' coursecore-api 2>/dev/null || echo missing)"
    if [ "$status" = "healthy" ]; then
      return 0
    fi
    sleep 3
  done
  return 1
}

COURSECORE_IMAGE="$NEW_IMAGE" docker compose pull coursecore-api
COURSECORE_IMAGE="$NEW_IMAGE" docker compose up -d

if wait_until_healthy; then
  echo "deploy ok: $NEW_IMAGE is healthy"
  docker image prune -f >/dev/null
  exit 0
fi

echo "::error::$NEW_IMAGE did not become healthy within ${HEALTH_TIMEOUT_SECONDS}s"
docker compose logs --tail 80 coursecore-api || true

if [ -n "$PREVIOUS_IMAGE" ] && [ "$PREVIOUS_IMAGE" != "$NEW_IMAGE" ]; then
  echo "rolling back to $PREVIOUS_IMAGE"
  COURSECORE_IMAGE="$PREVIOUS_IMAGE" docker compose up -d
  if wait_until_healthy; then
    echo "rollback ok"
  else
    echo "::error::rollback to $PREVIOUS_IMAGE is not healthy either"
  fi
fi

exit 1
