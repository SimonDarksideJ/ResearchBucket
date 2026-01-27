#!/usr/bin/env bash

################################################################################
# LiveKit - Logs Viewer
# View logs from LiveKit and related services
################################################################################

set -e

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m'

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Find storage path
find_storage_path() {
    if [ -f "$SCRIPT_DIR/../../../.storage_path" ]; then
        STORAGE_PATH=$(cat "$SCRIPT_DIR/../../../.storage_path")
    elif [ -n "$LIVEKIT_STORAGE" ]; then
        STORAGE_PATH="$LIVEKIT_STORAGE"
    else
        if [[ "$OSTYPE" == "darwin"* ]]; then
            for vol in /Volumes/*/livekit; do
                if [ -d "$vol" ] && [ -f "$vol/docker-compose.yml" ]; then
                    STORAGE_PATH="$vol"
                    break
                fi
            done
        else
            STORAGE_PATH="/opt/livekit-storage"
        fi
    fi
    
    if [ ! -d "$STORAGE_PATH" ]; then
        echo -e "${RED}❌ Storage path not found${NC}"
        exit 1
    fi
}

find_storage_path
cd "$STORAGE_PATH"

show_help() {
    cat << EOF
${CYAN}LiveKit Logs Viewer${NC}

Usage: $0 [SERVICE] [OPTIONS]

Services:
  livekit       LiveKit server logs
  prometheus    Prometheus logs
  grafana       Grafana logs
  caddy         Caddy reverse proxy logs
  loki          Loki log aggregator logs
  all           All services (default)

Options:
  --follow, -f        Follow log output
  --tail N            Show last N lines (default: 100)
  --since TIME        Show logs since timestamp (e.g., "1h", "30m", "2024-01-01")
  --help, -h          Show this help message

Examples:
  $0                      # Show last 100 lines from all services
  $0 livekit --follow     # Follow LiveKit logs
  $0 livekit --tail 500   # Show last 500 lines
  $0 all --since 1h       # Show logs from last hour
  $0 livekit | grep ERROR # Search for errors

EOF
}

# Parse arguments
SERVICE="${1:-all}"
FOLLOW=false
TAIL=100
SINCE=""

shift || true

while [[ $# -gt 0 ]]; do
    case $1 in
        --follow|-f)
            FOLLOW=true
            shift
            ;;
        --tail)
            TAIL="$2"
            shift 2
            ;;
        --since)
            SINCE="$2"
            shift 2
            ;;
        --help|-h)
            show_help
            exit 0
            ;;
        *)
            echo -e "${RED}Unknown option: $1${NC}"
            show_help
            exit 1
            ;;
    esac
done

# Build docker logs command
build_log_cmd() {
    local service=$1
    local cmd="docker compose logs"
    
    if [ "$FOLLOW" = true ]; then
        cmd="$cmd --follow"
    fi
    
    if [ -n "$TAIL" ] && [ "$FOLLOW" = false ]; then
        cmd="$cmd --tail $TAIL"
    fi
    
    if [ -n "$SINCE" ]; then
        cmd="$cmd --since $SINCE"
    fi
    
    cmd="$cmd $service"
    echo "$cmd"
}

# Show logs
case $SERVICE in
    livekit|prometheus|grafana|caddy|loki|promtail)
        echo -e "${CYAN}📋 Viewing $SERVICE logs...${NC}"
        echo ""
        eval "$(build_log_cmd $SERVICE)"
        ;;
    all)
        echo -e "${CYAN}📋 Viewing all service logs...${NC}"
        echo ""
        eval "$(build_log_cmd '')"
        ;;
    *)
        echo -e "${RED}Unknown service: $SERVICE${NC}"
        show_help
        exit 1
        ;;
esac
