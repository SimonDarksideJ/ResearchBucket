#!/usr/bin/env bash

################################################################################
# LiveKit Installation Script for Linux
#
# Provisions a single-node LiveKit stack for a public Linux host such as a
# Hetzner Cloud VM. This script assumes Ubuntu 22.04/24.04 or a similar
# systemd-based Debian-family environment.
#
# Usage: ./install-linux.sh [OPTIONS]
#
# Options:
#   --storage PATH          Installation path (default: /opt/livekit)
#   --env [dev|production]  Deployment environment
#   --domain DOMAIN         Public LiveKit hostname (required for production)
#   --email EMAIL           ACME email for Caddy (required for production)
#   --grafana-pass PASS     Grafana admin password
#   --skip-deps             Skip package installation
#   --help                  Show help
#
################################################################################

set -euo pipefail

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

DEFAULT_STORAGE_PATH="/opt/livekit"
DEFAULT_LIVEKIT_IMAGE="livekit/livekit-server:latest"
STORAGE_PATH=""
ENVIRONMENT="production"
DOMAIN=""
EMAIL=""
GRAFANA_PASSWORD=""
SKIP_DEPS=false
API_KEY=""
API_SECRET=""
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

log_info() { echo -e "${BLUE}[INFO]${NC} $1"; }
log_success() { echo -e "${GREEN}[OK]${NC} $1"; }
log_warning() { echo -e "${YELLOW}[WARN]${NC} $1"; }
log_error() { echo -e "${RED}[ERROR]${NC} $1"; }

show_help() {
    grep '^#' "$0" | sed 's/^# //' | sed 's/^#//'
}

parse_args() {
    while [[ $# -gt 0 ]]; do
        case "$1" in
            --storage)
                STORAGE_PATH="$2"
                shift 2
                ;;
            --env)
                ENVIRONMENT="$2"
                shift 2
                ;;
            --domain)
                DOMAIN="$2"
                shift 2
                ;;
            --email)
                EMAIL="$2"
                shift 2
                ;;
            --grafana-pass)
                GRAFANA_PASSWORD="$2"
                shift 2
                ;;
            --skip-deps)
                SKIP_DEPS=true
                shift
                ;;
            --help)
                show_help
                exit 0
                ;;
            *)
                log_error "Unknown option: $1"
                exit 1
                ;;
        esac
    done
}

require_root() {
    if [[ "$EUID" -ne 0 ]]; then
        log_error "Run this installer as root or via sudo."
        exit 1
    fi
}

validate_environment() {
    if [[ "$OSTYPE" != linux* ]]; then
        log_error "This script targets Linux hosts only."
        exit 1
    fi

    if [[ "$ENVIRONMENT" == "production" ]]; then
        if [[ -z "$DOMAIN" || -z "$EMAIL" ]]; then
            log_error "--domain and --email are required for production deployments."
            exit 1
        fi
    fi

    log_success "Environment validated"
}

install_dependencies() {
    if [[ "$SKIP_DEPS" == true ]]; then
        log_warning "Skipping package installation"
        return
    fi

    log_info "Installing Docker and supporting packages"
    apt-get update
    apt-get install -y ca-certificates curl docker.io docker-compose-plugin jq openssl ufw
    systemctl enable --now docker
    log_success "Dependencies installed"
}

setup_storage() {
    STORAGE_PATH="${STORAGE_PATH:-$DEFAULT_STORAGE_PATH}"

    mkdir -p "$STORAGE_PATH"/config
    mkdir -p "$STORAGE_PATH"/data/{caddy,grafana,loki,prometheus,redis}
    mkdir -p "$STORAGE_PATH"/logs/livekit
    mkdir -p "$STORAGE_PATH"/monitoring/{dashboards,grafana,loki,prometheus}
    mkdir -p "$STORAGE_PATH"/backups

    log_success "Prepared storage at $STORAGE_PATH"
}

copy_monitoring_assets() {
    cp -r "$PROJECT_ROOT/monitoring/." "$STORAGE_PATH/monitoring/"
    log_success "Copied monitoring assets"
}

generate_livekit_keys() {
    local output

    log_info "Generating LiveKit API keys"
    output="$(docker run --rm "$DEFAULT_LIVEKIT_IMAGE" generate-keys)"
    API_KEY="$(echo "$output" | awk '/API Key:/ {print $3}')"
    API_SECRET="$(echo "$output" | awk '/API Secret:/ {print $3}')"

    if [[ -z "$API_KEY" || -z "$API_SECRET" ]]; then
        log_error "Failed to generate LiveKit API keys"
        exit 1
    fi

    log_success "LiveKit API keys created"
}

generate_grafana_password() {
    if [[ -n "$GRAFANA_PASSWORD" ]]; then
        return
    fi

    GRAFANA_PASSWORD="$(openssl rand -base64 18 | tr -d '=+/' | cut -c1-18)"
    log_success "Generated Grafana password"
}

write_env_file() {
    cat > "$STORAGE_PATH/.env" <<EOF
LIVEKIT_IMAGE=$DEFAULT_LIVEKIT_IMAGE
REDIS_IMAGE=redis:7-alpine
CADDY_IMAGE=caddy:latest
PROMETHEUS_IMAGE=prom/prometheus:latest
GRAFANA_IMAGE=grafana/grafana:latest
LOKI_IMAGE=grafana/loki:latest
PROMTAIL_IMAGE=grafana/promtail:latest
NODE_EXPORTER_IMAGE=prom/node-exporter:latest
LIVEKIT_DOMAIN=$DOMAIN
ACME_EMAIL=$EMAIL
GF_SECURITY_ADMIN_USER=admin
GF_SECURITY_ADMIN_PASSWORD=$GRAFANA_PASSWORD
EOF

    chmod 600 "$STORAGE_PATH/.env"
    log_success "Wrote .env"
}

write_livekit_config() {
    cat > "$STORAGE_PATH/config/livekit.yaml" <<EOF
port: 7880
redis:
  address: redis:6379
rtc:
  tcp_port: 7881
  port_range_start: 50000
  port_range_end: 60000
  use_external_ip: true
turn:
  enabled: true
  udp_port: 3478
keys:
  $API_KEY: $API_SECRET
prometheus_port: 6789
room:
  empty_timeout: 300
  departure_timeout: 20
EOF

    log_success "Wrote livekit.yaml"
}

write_caddy_config() {
    cat > "$STORAGE_PATH/config/Caddyfile" <<'EOF'
{
    email {$ACME_EMAIL}
}

{$LIVEKIT_DOMAIN} {
    @health path /healthz
  respond @health "ok" 200
    reverse_proxy livekit:7880
}
EOF

    log_success "Wrote Caddyfile"
}

write_compose_file() {
    cat > "$STORAGE_PATH/docker-compose.yml" <<'EOF'
services:
  redis:
    image: ${REDIS_IMAGE}
    container_name: livekit-redis
    restart: unless-stopped
    command: redis-server --save 60 1000 --appendonly yes
    volumes:
      - ./data/redis:/data
    networks:
      - livekit-net

  livekit:
    image: ${LIVEKIT_IMAGE}
    container_name: livekit
    restart: unless-stopped
    command: --config /etc/livekit/livekit.yaml
    depends_on:
      - redis
    ports:
      - 127.0.0.1:7880:7880/tcp
      - 7881:7881/tcp
      - 3478:3478/udp
      - 50000-60000:50000-60000/udp
    volumes:
      - ./config/livekit.yaml:/etc/livekit/livekit.yaml:ro
      - ./logs/livekit:/var/log/livekit
    networks:
      - livekit-net
    logging:
      driver: json-file
      options:
        max-size: 10m
        max-file: '3'

  caddy:
    image: ${CADDY_IMAGE}
    container_name: livekit-caddy
    restart: unless-stopped
    depends_on:
      - livekit
    environment:
      LIVEKIT_DOMAIN: ${LIVEKIT_DOMAIN}
      ACME_EMAIL: ${ACME_EMAIL}
    ports:
      - 80:80/tcp
      - 443:443/tcp
    volumes:
      - ./config/Caddyfile:/etc/caddy/Caddyfile:ro
      - ./data/caddy:/data
    networks:
      - livekit-net

  prometheus:
    image: ${PROMETHEUS_IMAGE}
    container_name: prometheus
    restart: unless-stopped
    depends_on:
      - livekit
      - node-exporter
      - loki
      - promtail
    ports:
      - 127.0.0.1:9090:9090
    command:
      - --config.file=/etc/prometheus/prometheus.yml
      - --storage.tsdb.path=/prometheus
      - --storage.tsdb.retention.time=30d
      - --web.enable-lifecycle
    volumes:
      - ./monitoring/prometheus/prometheus.yml:/etc/prometheus/prometheus.yml:ro
      - ./monitoring/prometheus/alerts.yml:/etc/prometheus/alerts.yml:ro
      - ./data/prometheus:/prometheus
    networks:
      - livekit-net

  grafana:
    image: ${GRAFANA_IMAGE}
    container_name: grafana
    restart: unless-stopped
    depends_on:
      - prometheus
      - loki
    ports:
      - 127.0.0.1:3000:3000
    environment:
      GF_SECURITY_ADMIN_USER: ${GF_SECURITY_ADMIN_USER}
      GF_SECURITY_ADMIN_PASSWORD: ${GF_SECURITY_ADMIN_PASSWORD}
      GF_USERS_ALLOW_SIGN_UP: 'false'
      GF_SERVER_ROOT_URL: http://localhost:3000
    volumes:
      - ./monitoring/grafana/datasources.yml:/etc/grafana/provisioning/datasources/datasources.yml:ro
      - ./monitoring/grafana/dashboards.yml:/etc/grafana/provisioning/dashboards/dashboards.yml:ro
      - ./monitoring/dashboards:/var/lib/grafana/dashboards:ro
      - ./data/grafana:/var/lib/grafana
    networks:
      - livekit-net

  loki:
    image: ${LOKI_IMAGE}
    container_name: loki
    restart: unless-stopped
    ports:
      - 127.0.0.1:3100:3100
    command: -config.file=/etc/loki/local-config.yaml
    volumes:
      - ./monitoring/loki/loki-config.yml:/etc/loki/local-config.yaml:ro
      - ./data/loki:/loki
    networks:
      - livekit-net

  promtail:
    image: ${PROMTAIL_IMAGE}
    container_name: promtail
    restart: unless-stopped
    depends_on:
      - loki
    command: -config.file=/etc/promtail/config.yml
    volumes:
      - ./monitoring/loki/promtail-config.yml:/etc/promtail/config.yml:ro
      - ./logs:/var/log/livekit:ro
      - /var/run/docker.sock:/var/run/docker.sock:ro
    networks:
      - livekit-net

  node-exporter:
    image: ${NODE_EXPORTER_IMAGE}
    container_name: node-exporter
    restart: unless-stopped
    command:
      - --path.rootfs=/host
    volumes:
      - /:/host:ro,rslave
    networks:
      - livekit-net

networks:
  livekit-net:
    driver: bridge
EOF

    log_success "Wrote docker-compose.yml"
}

create_helper_scripts() {
    cat > "$STORAGE_PATH/start.sh" <<'EOF'
#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")"
docker compose up -d
docker compose ps
EOF

    cat > "$STORAGE_PATH/stop.sh" <<'EOF'
#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")"
docker compose down
EOF

    cat > "$STORAGE_PATH/status.sh" <<'EOF'
#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")"
docker compose ps
EOF

    cat > "$STORAGE_PATH/logs.sh" <<'EOF'
#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")"
if [[ $# -gt 0 ]]; then
  docker compose logs -f "$1"
else
  docker compose logs -f
fi
EOF

    chmod +x "$STORAGE_PATH"/{start.sh,stop.sh,status.sh,logs.sh}
    log_success "Created helper scripts"
}

start_services() {
    cd "$STORAGE_PATH"
    log_info "Pulling container images"
    docker compose pull

    log_info "Starting services"
    docker compose up -d

    if ! docker compose ps --status running | grep -q livekit; then
        log_error "LiveKit did not enter a running state"
        docker compose ps
        exit 1
    fi

    log_success "Linux stack is running"
}

write_credentials_file() {
    cat > "$STORAGE_PATH/.credentials" <<EOF
GENERATED_AT=$(date -u +%Y-%m-%dT%H:%M:%SZ)
LIVEKIT_API_KEY=$API_KEY
LIVEKIT_API_SECRET=$API_SECRET
GRAFANA_USERNAME=admin
GRAFANA_PASSWORD=$GRAFANA_PASSWORD
LIVEKIT_URL=https://$DOMAIN
GRAFANA_URL=http://127.0.0.1:3000
PROMETHEUS_URL=http://127.0.0.1:9090
LOKI_URL=http://127.0.0.1:3100
EOF

    chmod 600 "$STORAGE_PATH/.credentials"
    log_success "Wrote .credentials"
}

print_summary() {
    echo
    echo "============================================================"
    echo "LiveKit Linux stack installed"
    echo "============================================================"
    echo "Path:             $STORAGE_PATH"
    echo "Public URL:       https://$DOMAIN"
    echo "Grafana:          ssh -L 3000:127.0.0.1:3000 root@your-server"
    echo "Prometheus:       ssh -L 9090:127.0.0.1:9090 root@your-server"
    echo
    echo "Grafana user:     admin"
    echo "Grafana pass:     $GRAFANA_PASSWORD"
    echo "API key:          $API_KEY"
    echo "API secret:       $API_SECRET"
    echo
    echo "Open these firewall ports on the host or Hetzner Cloud Firewall:"
    echo "  80/tcp, 443/tcp, 7881/tcp, 3478/udp, 50000-60000/udp"
    echo
    echo "Note: This repo stack exposes direct UDP and TCP fallback. If you need"
    echo "TURN/TLS on 443 for strict corporate firewalls, use the official LiveKit"
    echo "VM generator as the source of truth for that edge case."
}

main() {
    parse_args "$@"
    require_root
    validate_environment
    install_dependencies
    setup_storage
    copy_monitoring_assets
    generate_livekit_keys
    generate_grafana_password
    write_env_file
    write_livekit_config
    write_caddy_config
    write_compose_file
    create_helper_scripts
    start_services
    write_credentials_file
    print_summary
}

main "$@"