#!/usr/bin/env bash

################################################################################
# LiveKit Installation Script for macOS
#
# Sets up a local LiveKit test stack on macOS. This install is suitable for
# local development and controlled remote testing once you add a real routed
# path for signaling and UDP media.
#
# Usage: ./install-mac.sh [OPTIONS]
#
# Options:
#   --storage PATH          Installation path (default: ~/livekit-data)
#   --skip-deps             Skip Homebrew dependency installation
#   --grafana-pass PASS     Grafana admin password
#   --help                  Show help
#
################################################################################

set -euo pipefail

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

DEFAULT_STORAGE_PATH="$HOME/livekit-data"
DEFAULT_LIVEKIT_IMAGE="livekit/livekit-server:latest"
STORAGE_PATH=""
SKIP_DEPS=false
GRAFANA_PASSWORD=""
API_KEY=""
API_SECRET=""
ARCH=""
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
                        --skip-deps)
                                SKIP_DEPS=true
                                shift
                                ;;
                        --grafana-pass)
                                GRAFANA_PASSWORD="$2"
                                shift 2
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

detect_platform() {
        if [[ "$OSTYPE" != darwin* ]]; then
                log_error "This script is for macOS only. Use deploy.sh for Linux."
                exit 1
        fi

        ARCH="$(uname -m)"
        log_info "Detected architecture: $ARCH"
}

check_prerequisites() {
        local os_version os_major

        os_version="$(sw_vers -productVersion)"
        os_major="$(echo "$os_version" | cut -d. -f1)"

        if [[ "$os_major" -lt 11 ]]; then
                log_error "macOS 11 or later is required. Found $os_version"
                exit 1
        fi

        log_success "macOS version: $os_version"
}

install_homebrew() {
        if command -v brew >/dev/null 2>&1; then
                log_success "Homebrew already installed"
                return
        fi

        log_info "Installing Homebrew"
        /bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"

        if [[ "$ARCH" == "arm64" ]]; then
                eval "$(/opt/homebrew/bin/brew shellenv)"
        fi

        log_success "Homebrew installed"
}

wait_for_docker() {
        local attempt

        for attempt in {1..30}; do
                if docker info >/dev/null 2>&1; then
                        log_success "Docker Desktop is running"
                        return
                fi

                sleep 2
        done

        log_error "Docker Desktop did not become ready in time"
        log_error "If this is the first launch, approve any Docker Desktop prompts and rerun the installer."
        exit 1
}

install_dependencies() {
        if [[ "$SKIP_DEPS" == true ]]; then
                log_warning "Skipping Homebrew dependency installation"
        else
                log_info "Installing required Homebrew packages"
                brew update
                brew install jq yq cloudflared
        fi

        if ! command -v docker >/dev/null 2>&1; then
                log_error "Docker Desktop is required."
                log_info "Install it from https://www.docker.com/products/docker-desktop, launch it once, approve any first-run prompts, then rerun this script."
                exit 1
        fi

        if ! docker info >/dev/null 2>&1; then
                log_info "Starting Docker Desktop"
                open -a Docker
                wait_for_docker
        else
                log_success "Docker Desktop is running"
        fi
}

setup_storage() {
        local parent_dir

        if [[ -z "$STORAGE_PATH" ]]; then
                STORAGE_PATH="$DEFAULT_STORAGE_PATH"
                log_info "No --storage path supplied; using the internal default: $STORAGE_PATH"
                log_info "Use --storage \"/Volumes/DriveName/livekit\" only when the external drive is mounted."
        fi

        parent_dir="$(dirname "$STORAGE_PATH")"

        if [[ ! -d "$parent_dir" ]]; then
                log_error "Parent directory does not exist: $parent_dir"
                log_info "If you want the simplest first run, omit --storage and use the internal default: $DEFAULT_STORAGE_PATH"
                log_info "For an external disk, use a mounted path such as /Volumes/SamsungT7/livekit"
                if [[ -d /Volumes ]]; then
                        log_info "Mounted volumes under /Volumes:"
                        ls -1 /Volumes
                fi
                exit 1
        fi

        mkdir -p "$STORAGE_PATH"/config
        mkdir -p "$STORAGE_PATH"/data/{grafana,loki,prometheus,redis}
        mkdir -p "$STORAGE_PATH"/logs/livekit
        mkdir -p "$STORAGE_PATH"/monitoring/{dashboards,grafana,loki,prometheus}
        mkdir -p "$STORAGE_PATH"/backups

        if [[ -L "$HOME/livekit" ]]; then
                rm -f "$HOME/livekit"
        elif [[ -e "$HOME/livekit" ]]; then
                log_error "$HOME/livekit already exists and is not a symlink. Move it aside before running the installer."
                exit 1
        fi
        ln -s "$STORAGE_PATH" "$HOME/livekit"

        log_success "Prepared storage at $STORAGE_PATH"
}

copy_monitoring_assets() {
        cp -r "$PROJECT_ROOT/monitoring/." "$STORAGE_PATH/monitoring/"

        cat > "$STORAGE_PATH/monitoring/prometheus/prometheus.yml" <<'EOF'
global:
    scrape_interval: 15s
    evaluation_interval: 15s
    external_labels:
        cluster: 'livekit-mac'
        environment: 'test'

rule_files:
    - alerts.yml

scrape_configs:
    - job_name: 'livekit'
        static_configs:
            - targets: ['livekit:6789']
                labels:
                    service: 'livekit'

    - job_name: 'prometheus'
        static_configs:
            - targets: ['localhost:9090']
                labels:
                    service: 'prometheus'

    - job_name: 'loki'
        static_configs:
            - targets: ['loki:3100']
                labels:
                    service: 'loki'

    - job_name: 'promtail'
        static_configs:
            - targets: ['promtail:9080']
                labels:
                    service: 'promtail'
EOF

        cat > "$STORAGE_PATH/monitoring/prometheus/alerts.yml" <<'EOF'
groups:
    - name: livekit_test_stack
        interval: 30s
        rules:
            - alert: LiveKitDown
                expr: up{job="livekit"} == 0
                for: 1m
                labels:
                    severity: critical
                annotations:
                    summary: "LiveKit is down"
                    description: "The macOS test LiveKit container has been unreachable for more than 1 minute."

            - alert: LokiDown
                expr: up{job="loki"} == 0
                for: 2m
                labels:
                    severity: warning
                annotations:
                    summary: "Loki is down"
                    description: "The log aggregation service is unavailable."

            - alert: PromtailDown
                expr: up{job="promtail"} == 0
                for: 2m
                labels:
                    severity: warning
                annotations:
                    summary: "Promtail is down"
                    description: "Log shipping from Docker containers has stopped."
EOF

        log_success "Installed macOS-specific monitoring configuration"
}

generate_livekit_keys() {
        local output

        log_info "Generating LiveKit API keys"
        output="$(docker run --rm "$DEFAULT_LIVEKIT_IMAGE" generate-keys)"

        API_KEY="$(echo "$output" | awk '/API Key:/ {print $3}')"
        API_SECRET="$(echo "$output" | awk '/API Secret:/ {print $3}')"

        if [[ -z "$API_KEY" || -z "$API_SECRET" ]]; then
                log_error "Failed to parse generated API keys"
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
PROMETHEUS_IMAGE=prom/prometheus:latest
GRAFANA_IMAGE=grafana/grafana:latest
LOKI_IMAGE=grafana/loki:latest
PROMTAIL_IMAGE=grafana/promtail:latest
GF_SECURITY_ADMIN_USER=admin
GF_SECURITY_ADMIN_PASSWORD=$GRAFANA_PASSWORD
EOF

        chmod 600 "$STORAGE_PATH/.env"
        log_success "Wrote image and credential defaults to .env"
}

write_livekit_config() {
        cat > "$STORAGE_PATH/config/livekit.yaml" <<EOF
port: 7880
redis:
    address: redis:6379
rtc:
    tcp_port: 7881
    port_range_start: 50000
    port_range_end: 50100
    # For direct router exposure, uncomment the next line.
    # use_external_ip: true
    # For an edge relay over WireGuard, set use_external_ip to false and define node_ip.
    # node_ip: 10.20.0.2
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
            - 50000-50100:50000-50100/udp
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

    prometheus:
        image: ${PROMETHEUS_IMAGE}
        container_name: prometheus
        restart: unless-stopped
        depends_on:
            - livekit
        ports:
            - 127.0.0.1:9090:9090
        command:
            - --config.file=/etc/prometheus/prometheus.yml
            - --storage.tsdb.path=/prometheus
            - --storage.tsdb.retention.time=15d
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

        log_success "Local stack is running"
}

write_credentials_file() {
        cat > "$STORAGE_PATH/.credentials" <<EOF
GENERATED_AT=$(date -u +%Y-%m-%dT%H:%M:%SZ)
LIVEKIT_API_KEY=$API_KEY
LIVEKIT_API_SECRET=$API_SECRET
GRAFANA_USERNAME=admin
GRAFANA_PASSWORD=$GRAFANA_PASSWORD
LIVEKIT_URL=http://localhost:7880
GRAFANA_URL=http://localhost:3000
PROMETHEUS_URL=http://localhost:9090
LOKI_URL=http://localhost:3100
EOF

        chmod 600 "$STORAGE_PATH/.credentials"
        log_success "Wrote .credentials"
}

print_summary() {
        echo
        echo "============================================================"
        echo "LiveKit macOS test stack installed"
        echo "============================================================"
        echo "Path:            $STORAGE_PATH"
        echo "Shortcut:        ~/livekit"
        echo "LiveKit:         http://localhost:7880"
        echo "Grafana:         http://localhost:3000"
        echo "Prometheus:      http://localhost:9090"
        echo "Loki:            http://localhost:3100"
        echo "Credentials:     $STORAGE_PATH/.credentials"
        echo
        echo "Grafana user:    admin"
        echo "Grafana pass:    $GRAFANA_PASSWORD"
        echo "API key:         $API_KEY"
        echo "API secret:      $API_SECRET"
        echo
        echo "For external storage instead of the internal default, rerun with:"
        echo "  ./scripts/install-mac.sh --storage \"/Volumes/DriveName/livekit\""
        echo
        echo "Important: Cloudflare Tunnel can expose admin or signaling endpoints,"
        echo "but it does not replace the routed UDP media path LiveKit needs."
        echo "For remote browser tests, use either router port forwarding or a public"
        echo "edge relay/VPS with WireGuard plus TCP/UDP forwarding."
        echo
        echo "Documentation:   07-mac-deployment.md"
}

main() {
        parse_args "$@"
        detect_platform
        check_prerequisites
        install_homebrew
        install_dependencies
        setup_storage
        copy_monitoring_assets
        generate_livekit_keys
        generate_grafana_password
        write_env_file
        write_livekit_config
        write_compose_file
        create_helper_scripts
        start_services
        write_credentials_file
        print_summary
}

main "$@"
