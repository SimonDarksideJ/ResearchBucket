#!/usr/bin/env bash

################################################################################
# Health Check Script for LiveKit Deployment
#
# Performs a lightweight status check against the local Docker Compose stack.
#
# Usage: ./health-check.sh [OPTIONS]
#
# Options:
#   --verbose    Show recent error lines from docker compose logs
#   --json       Output machine-readable JSON
#   --help       Show help
#
################################################################################

set -euo pipefail

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

VERBOSE=false
JSON_OUTPUT=false

show_help() {
    grep '^#' "$0" | sed 's/^# //' | sed 's/^#//'
}

while [[ $# -gt 0 ]]; do
    case "$1" in
        --verbose)
            VERBOSE=true
            shift
            ;;
        --json)
            JSON_OUTPUT=true
            shift
            ;;
        --help)
            show_help
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done

if [[ -L "$HOME/livekit" ]]; then
    LIVEKIT_DIR="$HOME/livekit"
elif [[ -d "/opt/livekit" ]]; then
    LIVEKIT_DIR="/opt/livekit"
else
    echo "LiveKit installation not found"
    exit 1
fi

cd "$LIVEKIT_DIR"

if ! docker info >/dev/null 2>&1; then
    echo "Docker is not running"
    exit 1
fi

mapfile -t SERVICES < <(docker compose config --services)
RUNNING_SERVICES="$(docker compose ps --status running --services || true)"
ERROR_COUNT="$(docker compose logs --since=1h 2>&1 | grep -i "error" | wc -l | tr -d ' ')"

check_service() {
    local service="$1"

    if echo "$RUNNING_SERVICES" | grep -qx "$service"; then
        return 0
    fi

    return 1
}

check_endpoint() {
    local url="$1"

    if curl -sS "$url" >/dev/null 2>&1; then
        return 0
    fi

    return 1
}

if [[ "$JSON_OUTPUT" == true ]]; then
    printf '{"docker":"running","services":{'

    for index in "${!SERVICES[@]}"; do
        service="${SERVICES[$index]}"
        state="stopped"
        if check_service "$service"; then
            state="running"
        fi

        printf '"%s":"%s"' "$service" "$state"
        if [[ "$index" -lt $((${#SERVICES[@]} - 1)) ]]; then
            printf ','
        fi
    done

    printf '},"endpoints":{' 
    printf '"livekit":"%s",' "$(check_endpoint http://localhost:7880 && echo ok || echo fail)"
    printf '"grafana":"%s",' "$(check_endpoint http://localhost:3000/api/health && echo ok || echo fail)"
    printf '"prometheus":"%s",' "$(check_endpoint http://localhost:9090/-/healthy && echo ok || echo fail)"
    printf '"loki":"%s"' "$(check_endpoint http://localhost:3100/ready && echo ok || echo fail)"
    printf '},"errorCount":%s}\n' "$ERROR_COUNT"
    exit 0
fi

echo "============================================================"
echo "LiveKit Health Check"
echo "============================================================"
echo
echo "Docker: running"
echo
echo "Services:"
for service in "${SERVICES[@]}"; do
    if check_service "$service"; then
        echo "  [OK]   $service"
    else
        echo "  [FAIL] $service"
    fi
done

echo
echo "Endpoints:"
for line in \
    "LiveKit|http://localhost:7880" \
    "Grafana|http://localhost:3000/api/health" \
    "Prometheus|http://localhost:9090/-/healthy" \
    "Loki|http://localhost:3100/ready"; do
    name="${line%%|*}"
    url="${line##*|}"
    if check_endpoint "$url"; then
        echo "  [OK]   $name"
    else
        echo "  [FAIL] $name ($url)"
    fi
done

echo
echo "Recent log errors (last hour): $ERROR_COUNT"
if [[ "$VERBOSE" == true && "$ERROR_COUNT" -gt 0 ]]; then
    docker compose logs --since=1h 2>&1 | grep -i "error" | tail -10
    echo
fi

echo "Disk usage:"
df -h "$LIVEKIT_DIR" | tail -1

echo
echo "Container usage:"
docker stats --no-stream --format "table {{.Name}}\t{{.CPUPerc}}\t{{.MemUsage}}"
