#!/usr/bin/env bash

################################################################################
# LiveKit - Health Check Script
# Comprehensive health monitoring for LiveKit deployment
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
}

find_storage_path

# Health check results
HEALTH_PASSED=0
HEALTH_FAILED=0
HEALTH_WARNINGS=0

check_pass() {
    echo -e "  ${GREEN}✅${NC} $1"
    ((HEALTH_PASSED++))
}

check_fail() {
    echo -e "  ${RED}❌${NC} $1"
    ((HEALTH_FAILED++))
}

check_warn() {
    echo -e "  ${YELLOW}⚠️${NC}  $1"
    ((HEALTH_WARNINGS++))
}

echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${CYAN}🏥 LiveKit Health Check${NC}"
echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo ""

# Check Docker
echo -e "${BLUE}🐳 Docker Status:${NC}"
if docker ps >/dev/null 2>&1; then
    check_pass "Docker daemon running"
else
    check_fail "Docker daemon not running"
fi

# Check containers
echo ""
echo -e "${BLUE}📦 Container Status:${NC}"

containers=("livekit" "livekit-caddy" "livekit-prometheus" "livekit-grafana" "livekit-loki")

for container in "${containers[@]}"; do
    if docker ps --format "{{.Names}}" | grep -q "^${container}$"; then
        status=$(docker inspect "$container" --format='{{.State.Status}}')
        if [ "$status" = "running" ]; then
            check_pass "$container: Running"
        else
            check_fail "$container: $status"
        fi
    else
        check_warn "$container: Not found (may be optional)"
    fi
done

# Check LiveKit API
echo ""
echo -e "${BLUE}🌐 LiveKit API:${NC}"

if response=$(curl -f -s -w "\n%{http_code}" http://localhost:7880/health 2>/dev/null); then
    http_code=$(echo "$response" | tail -n1)
    if [ "$http_code" = "200" ]; then
        response_time=$(curl -o /dev/null -s -w '%{time_total}' http://localhost:7880/health 2>/dev/null)
        check_pass "API responding (${response_time}s)"
    else
        check_fail "API returned HTTP $http_code"
    fi
else
    check_fail "API not responding"
fi

# Check WebSocket
echo ""
echo -e "${BLUE}🔌 WebSocket Connection:${NC}"

if command -v wscat >/dev/null 2>&1; then
    if timeout 5 wscat -c ws://localhost:7880/ --execute "ping" >/dev/null 2>&1; then
        check_pass "WebSocket connection OK"
    else
        check_warn "WebSocket connection test inconclusive"
    fi
else
    check_warn "wscat not installed, skipping WebSocket test"
fi

# Check Prometheus
echo ""
echo -e "${BLUE}📊 Prometheus Metrics:${NC}"

if curl -f -s http://localhost:9090/-/healthy >/dev/null 2>&1; then
    check_pass "Prometheus healthy"
    
    # Check if scraping LiveKit
    targets=$(curl -s http://localhost:9090/api/v1/targets 2>/dev/null | jq '.data.activeTargets | length' 2>/dev/null || echo "0")
    if [ "$targets" -gt 0 ]; then
        check_pass "Prometheus scraping $targets targets"
    else
        check_warn "Prometheus not scraping any targets"
    fi
else
    check_warn "Prometheus not responding"
fi

# Check Grafana
echo ""
echo -e "${BLUE}📈 Grafana Dashboard:${NC}"

if curl -f -s http://localhost:3000/api/health >/dev/null 2>&1; then
    check_pass "Grafana accessible"
else
    check_warn "Grafana not responding"
fi

# Check storage
echo ""
echo -e "${BLUE}💾 Storage Status:${NC}"

if [ -d "$STORAGE_PATH" ]; then
    check_pass "Storage path exists: $STORAGE_PATH"
    
    # Check write access
    if touch "$STORAGE_PATH/.health_test" 2>/dev/null && rm "$STORAGE_PATH/.health_test" 2>/dev/null; then
        check_pass "Storage is writable"
    else
        check_fail "Storage is not writable"
    fi
    
    # Check disk space
    available=$(df -h "$STORAGE_PATH" 2>/dev/null | awk 'NR==2 {print $4}')
    used_percent=$(df -h "$STORAGE_PATH" 2>/dev/null | awk 'NR==2 {print $5}' | tr -d '%')
    
    if [ -n "$used_percent" ]; then
        if [ "$used_percent" -lt 80 ]; then
            check_pass "Disk space adequate ($available free)"
        elif [ "$used_percent" -lt 90 ]; then
            check_warn "Disk space getting low ($available free)"
        else
            check_fail "Disk space critical ($available free)"
        fi
    fi
else
    check_fail "Storage path not found"
fi

# Check system resources
echo ""
echo -e "${BLUE}💻 System Resources:${NC}"

# CPU usage
if [[ "$OSTYPE" == "darwin"* ]]; then
    cpu_usage=$(ps aux | grep livekit-server | grep -v grep | awk '{print $3}' | head -1 || echo "0")
else
    cpu_usage=$(docker stats --no-stream --format "{{.CPUPerc}}" livekit 2>/dev/null | tr -d '%' || echo "0")
fi

if [ -n "$cpu_usage" ]; then
    if (( $(echo "$cpu_usage < 70" | bc -l 2>/dev/null || echo "1") )); then
        check_pass "CPU usage normal (${cpu_usage}%)"
    elif (( $(echo "$cpu_usage < 85" | bc -l 2>/dev/null || echo "1") )); then
        check_warn "CPU usage elevated (${cpu_usage}%)"
    else
        check_fail "CPU usage high (${cpu_usage}%)"
    fi
fi

# Memory usage
if [[ "$OSTYPE" == "darwin"* ]]; then
    mem_usage=$(ps aux | grep livekit-server | grep -v grep | awk '{print $4}' | head -1 || echo "0")
else
    mem_usage=$(docker stats --no-stream --format "{{.MemPerc}}" livekit 2>/dev/null | tr -d '%' || echo "0")
fi

if [ -n "$mem_usage" ]; then
    if (( $(echo "$mem_usage < 70" | bc -l 2>/dev/null || echo "1") )); then
        check_pass "Memory usage normal (${mem_usage}%)"
    elif (( $(echo "$mem_usage < 85" | bc -l 2>/dev/null || echo "1") )); then
        check_warn "Memory usage elevated (${mem_usage}%)"
    else
        check_fail "Memory usage high (${mem_usage}%)"
    fi
fi

# Check for recent errors in logs
echo ""
echo -e "${BLUE}📝 Recent Errors:${NC}"

if [ -f "$STORAGE_PATH/logs/livekit.log" ]; then
    error_count=$(grep -i "error\|fatal" "$STORAGE_PATH/logs/livekit.log" 2>/dev/null | tail -100 | wc -l | tr -d ' ')
    if [ "$error_count" -eq 0 ]; then
        check_pass "No recent errors in logs"
    elif [ "$error_count" -lt 5 ]; then
        check_warn "$error_count errors found in last 100 log lines"
    else
        check_fail "$error_count errors found in last 100 log lines"
    fi
else
    check_warn "Log file not found"
fi

# Network connectivity
echo ""
echo -e "${BLUE}🌍 Network Connectivity:${NC}"

if ping -c 1 8.8.8.8 >/dev/null 2>&1; then
    check_pass "Internet connectivity OK"
else
    check_warn "Internet connectivity check failed"
fi

# Summary
echo ""
echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${CYAN}📊 Health Check Summary${NC}"
echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "  ${GREEN}✅ Passed:${NC} $HEALTH_PASSED"
echo -e "  ${YELLOW}⚠️  Warnings:${NC} $HEALTH_WARNINGS"
echo -e "  ${RED}❌ Failed:${NC} $HEALTH_FAILED"
echo ""

if [ $HEALTH_FAILED -gt 0 ]; then
    echo -e "${RED}⚠️  System has critical issues that need attention${NC}"
    exit 1
elif [ $HEALTH_WARNINGS -gt 0 ]; then
    echo -e "${YELLOW}⚠️  System has warnings but is operational${NC}"
    exit 0
else
    echo -e "${GREEN}✅ All health checks passed${NC}"
    exit 0
fi
