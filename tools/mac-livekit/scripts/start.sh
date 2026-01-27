#!/usr/bin/env bash

################################################################################
# LiveKit - Service Management Scripts
# Start, stop, restart, and status check for LiveKit services
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
    # Check for .env file
    if [ -f "$SCRIPT_DIR/../../../.storage_path" ]; then
        STORAGE_PATH=$(cat "$SCRIPT_DIR/../../../.storage_path")
    elif [ -n "$LIVEKIT_STORAGE" ]; then
        STORAGE_PATH="$LIVEKIT_STORAGE"
    else
        # Try to detect
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
        echo -e "${RED}❌ Storage path not found. Run setup.sh first.${NC}"
        exit 1
    fi
}

find_storage_path

cd "$STORAGE_PATH"

# Start services
start_services() {
    echo -e "${BLUE}🚀 Starting LiveKit services...${NC}"
    docker compose up -d
    echo -e "${GREEN}✅ Services started${NC}"
    
    sleep 5
    check_health
}

# Stop services
stop_services() {
    echo -e "${YELLOW}⏹️  Stopping LiveKit services...${NC}"
    docker compose down
    echo -e "${GREEN}✅ Services stopped${NC}"
}

# Restart services
restart_services() {
    echo -e "${BLUE}🔄 Restarting LiveKit services...${NC}"
    
    # Check for --graceful flag
    if [[ "$1" == "--graceful" ]]; then
        echo -e "${CYAN}ℹ️  Waiting for empty rooms...${NC}"
        
        # Wait for rooms to be empty (max 30 minutes)
        local max_wait=1800
        local elapsed=0
        
        while [ $elapsed -lt $max_wait ]; do
            local rooms=$(curl -s http://localhost:7880/rooms 2>/dev/null | jq '.rooms | length' 2>/dev/null || echo "0")
            
            if [ "$rooms" -eq 0 ]; then
                echo -e "${GREEN}✅ No active rooms, restarting...${NC}"
                break
            fi
            
            echo -e "${YELLOW}⏳ $rooms active rooms, waiting...${NC}"
            sleep 60
            elapsed=$((elapsed + 60))
        done
        
        if [ $elapsed -ge $max_wait ]; then
            echo -e "${RED}⚠️  Timeout reached, forcing restart${NC}"
        fi
    fi
    
    docker compose restart
    echo -e "${GREEN}✅ Services restarted${NC}"
    
    sleep 5
    check_health
}

# Check service status
check_status() {
    echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo -e "${CYAN}📊 LiveKit Status Report${NC}"
    echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo ""
    
    # Check containers
    echo -e "${BLUE}🐳 Container Status:${NC}"
    docker compose ps
    echo ""
    
    # Check health
    check_health
    
    # Get stats
    local rooms=$(curl -s http://localhost:7880/rooms 2>/dev/null | jq '.rooms | length' 2>/dev/null || echo "N/A")
    local participants=0
    
    if [ "$rooms" != "N/A" ] && [ "$rooms" -gt 0 ]; then
        participants=$(curl -s http://localhost:7880/rooms 2>/dev/null | jq '[.rooms[].num_participants] | add' 2>/dev/null || echo "N/A")
    fi
    
    echo -e "${BLUE}📈 Current Load:${NC}"
    echo -e "  Active Rooms: ${YELLOW}${rooms}${NC}"
    echo -e "  Total Participants: ${YELLOW}${participants}${NC}"
    
    # Storage usage
    local storage_used=$(du -sh "$STORAGE_PATH" 2>/dev/null | cut -f1 || echo "N/A")
    local storage_avail=$(df -h "$STORAGE_PATH" 2>/dev/null | awk 'NR==2 {print $4}' || echo "N/A")
    
    echo -e "  Storage Used: ${YELLOW}${storage_used}${NC}"
    echo -e "  Storage Available: ${YELLOW}${storage_avail}${NC}"
    
    # Uptime
    local uptime=$(docker inspect livekit --format='{{.State.StartedAt}}' 2>/dev/null || echo "N/A")
    if [ "$uptime" != "N/A" ]; then
        echo -e "  Container Started: ${YELLOW}${uptime}${NC}"
    fi
    
    echo ""
}

# Health check
check_health() {
    echo -e "${BLUE}🏥 Health Checks:${NC}"
    
    # LiveKit
    if curl -f -s http://localhost:7880/health > /dev/null 2>&1; then
        echo -e "  ${GREEN}✅${NC} LiveKit Server: ${GREEN}Healthy${NC}"
    else
        echo -e "  ${RED}❌${NC} LiveKit Server: ${RED}Unhealthy${NC}"
    fi
    
    # Prometheus
    if docker ps --filter "name=livekit-prometheus" --format "{{.Status}}" | grep -q "Up"; then
        if curl -f -s http://localhost:9090/-/healthy > /dev/null 2>&1; then
            echo -e "  ${GREEN}✅${NC} Prometheus: ${GREEN}Healthy${NC}"
        else
            echo -e "  ${YELLOW}⚠️${NC}  Prometheus: ${YELLOW}Starting...${NC}"
        fi
    fi
    
    # Grafana
    if docker ps --filter "name=livekit-grafana" --format "{{.Status}}" | grep -q "Up"; then
        if curl -f -s http://localhost:3000/api/health > /dev/null 2>&1; then
            echo -e "  ${GREEN}✅${NC} Grafana: ${GREEN}Healthy${NC}"
        else
            echo -e "  ${YELLOW}⚠️${NC}  Grafana: ${YELLOW}Starting...${NC}"
        fi
    fi
    
    echo ""
}

# Main script logic
case "${1:-status}" in
    start)
        start_services
        ;;
    stop)
        stop_services
        ;;
    restart)
        restart_services "$2"
        ;;
    status)
        check_status
        ;;
    health)
        check_health
        ;;
    *)
        echo "Usage: $0 {start|stop|restart [--graceful]|status|health}"
        exit 1
        ;;
esac
