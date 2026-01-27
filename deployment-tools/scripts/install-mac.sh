#!/usr/bin/env bash

################################################################################
# LiveKit Installation Script for macOS
# 
# This script automates the complete installation of LiveKit media server
# with monitoring stack on macOS with external storage support.
#
# Usage: ./install-mac.sh [OPTIONS]
#
# Options:
#   --storage PATH    Specify external storage path (default: /Volumes/External/livekit)
#   --skip-deps       Skip dependency installation
#   --grafana-pass    Set Grafana admin password (default: auto-generated)
#   --help            Show this help message
#
################################################################################

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Default configuration
DEFAULT_STORAGE_PATH="/Volumes/External/livekit"
SKIP_DEPS=false
GRAFANA_PASSWORD=""
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

# Logging functions
log_info() {
    echo -e "${BLUE}ℹ${NC} $1"
}

log_success() {
    echo -e "${GREEN}✓${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}⚠${NC} $1"
}

log_error() {
    echo -e "${RED}✗${NC} $1"
}

# Parse command line arguments
parse_args() {
    while [[ $# -gt 0 ]]; do
        case $1 in
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
                grep '^#' "$0" | sed 's/^# //' | sed 's/^#//'
                exit 0
                ;;
            *)
                log_error "Unknown option: $1"
                exit 1
                ;;
        esac
    done
}

# Detect platform
detect_platform() {
    if [[ "$OSTYPE" != "darwin"* ]]; then
        log_error "This script is for macOS only. Use deploy.sh for Linux."
        exit 1
    fi
    
    # Detect architecture
    ARCH=$(uname -m)
    if [[ "$ARCH" == "arm64" ]]; then
        log_info "Detected Apple Silicon (M1/M2)"
        PLATFORM="darwin-arm64"
    else
        log_info "Detected Intel Mac"
        PLATFORM="darwin-amd64"
    fi
}

# Check prerequisites
check_prerequisites() {
    log_info "Checking prerequisites..."
    
    # Check macOS version
    OS_VERSION=$(sw_vers -productVersion)
    OS_MAJOR=$(echo "$OS_VERSION" | cut -d. -f1)
    
    if [[ $OS_MAJOR -lt 11 ]]; then
        log_error "macOS 11.0 (Big Sur) or later is required. You have $OS_VERSION"
        exit 1
    fi
    
    log_success "macOS version: $OS_VERSION"
}

# Install Homebrew if not present
install_homebrew() {
    if ! command -v brew &> /dev/null; then
        log_info "Installing Homebrew..."
        /bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
        
        # Add Homebrew to PATH for Apple Silicon
        if [[ "$ARCH" == "arm64" ]]; then
            echo 'eval "$(/opt/homebrew/bin/brew shellenv)"' >> ~/.zprofile
            eval "$(/opt/homebrew/bin/brew shellenv)"
        fi
        
        log_success "Homebrew installed"
    else
        log_success "Homebrew already installed"
    fi
}

# Install required dependencies
install_dependencies() {
    if [[ "$SKIP_DEPS" == true ]]; then
        log_warning "Skipping dependency installation"
        return
    fi
    
    log_info "Installing required dependencies..."
    
    # Update Homebrew
    brew update
    
    # Install packages
    local packages=(
        "jq"
        "yq"
        "wget"
        "cloudflared"
    )
    
    for package in "${packages[@]}"; do
        if brew list "$package" &>/dev/null; then
            log_success "$package already installed"
        else
            log_info "Installing $package..."
            brew install "$package"
            log_success "$package installed"
        fi
    done
    
    # Install Docker Desktop if not present
    if ! command -v docker &> /dev/null; then
        log_info "Docker Desktop not found. Please install Docker Desktop manually:"
        log_info "1. Download from: https://www.docker.com/products/docker-desktop"
        log_info "2. Install and start Docker Desktop"
        log_info "3. Re-run this script"
        exit 1
    else
        # Check if Docker is running
        if ! docker ps &>/dev/null; then
            log_warning "Docker is installed but not running"
            log_info "Starting Docker Desktop..."
            open -a Docker
            log_info "Waiting for Docker to start (this may take a minute)..."
            
            # Wait for Docker to be ready
            for i in {1..30}; do
                if docker ps &>/dev/null; then
                    log_success "Docker is running"
                    break
                fi
                sleep 2
                echo -n "."
            done
            
            if ! docker ps &>/dev/null; then
                log_error "Docker failed to start. Please start Docker Desktop manually and re-run this script."
                exit 1
            fi
        else
            log_success "Docker is running"
        fi
    fi
}

# Setup external storage
setup_storage() {
    log_info "Setting up storage at: $STORAGE_PATH"
    
    # Check if path exists or parent exists
    PARENT_DIR=$(dirname "$STORAGE_PATH")
    
    if [[ ! -d "$PARENT_DIR" ]]; then
        log_error "Parent directory does not exist: $PARENT_DIR"
        log_error "Please ensure your external drive is connected and mounted"
        log_info "Available volumes:"
        ls -1 /Volumes/
        exit 1
    fi
    
    # Create directory structure
    mkdir -p "$STORAGE_PATH"/{config,data,logs,monitoring,backups}
    mkdir -p "$STORAGE_PATH"/monitoring/{dashboards,prometheus,grafana,loki}
    mkdir -p "$STORAGE_PATH"/data/{livekit,caddy,prometheus,grafana,loki}
    mkdir -p "$STORAGE_PATH"/logs/livekit
    
    log_success "Storage structure created"
    
    # Create symlink to home directory for easy access
    if [[ ! -L "$HOME/livekit" ]]; then
        ln -s "$STORAGE_PATH" "$HOME/livekit"
        log_success "Created symlink: ~/livekit -> $STORAGE_PATH"
    fi
}

# Generate LiveKit API keys
generate_livekit_keys() {
    log_info "Generating LiveKit API keys..."
    
    local keys_output
    keys_output=$(docker run --rm livekit/livekit-server:latest generate-keys)
    
    # Extract keys
    API_KEY=$(echo "$keys_output" | grep "API Key:" | awk '{print $3}')
    API_SECRET=$(echo "$keys_output" | grep "API Secret:" | awk '{print $3}')
    
    if [[ -z "$API_KEY" ]] || [[ -z "$API_SECRET" ]]; then
        log_error "Failed to generate API keys"
        exit 1
    fi
    
    log_success "API keys generated"
}

# Generate random Grafana password
generate_grafana_password() {
    if [[ -z "$GRAFANA_PASSWORD" ]]; then
        GRAFANA_PASSWORD=$(openssl rand -base64 12 | tr -d "=+/" | cut -c1-16)
        log_info "Generated Grafana password"
    fi
}

# Create configuration files
create_configs() {
    log_info "Creating configuration files..."
    
    # Copy configuration templates from project
    if [[ -d "$PROJECT_ROOT/configs" ]]; then
        cp -r "$PROJECT_ROOT/configs"/* "$STORAGE_PATH/config/" 2>/dev/null || true
    fi
    
    # Create LiveKit configuration
    cat > "$STORAGE_PATH/config/livekit.yaml" <<EOF
port: 7880
bind_addresses:
  - "0.0.0.0"

rtc:
  tcp_port: 7881
  port_range_start: 50000
  port_range_end: 50200
  use_external_ip: true
  ice_servers:
    - urls:
        - stun:stun.l.google.com:19302

keys:
  $API_KEY: $API_SECRET

logging:
  level: info
  sample: false
  pion_level: warn
  file:
    filename: /logs/livekit.log
    max_size: 100
    max_backups: 3

room:
  auto_create: true
  max_participants: 100
  empty_timeout: 300
  departure_timeout: 20

region: "mac-local"

development: false

prometheus:
  port: 6789
EOF
    
    log_success "Created livekit.yaml"
    
    # Create Caddyfile
    cat > "$STORAGE_PATH/config/Caddyfile" <<'EOF'
{
    local_certs
    auto_https off
}

:80 {
    handle_path /livekit/* {
        reverse_proxy livekit:7880 {
            header_up Host {host}
            header_up X-Real-IP {remote}
        }
    }
    
    handle_path /grafana/* {
        reverse_proxy grafana:3000
    }
    
    handle /ws* {
        reverse_proxy livekit:7880 {
            header_up Upgrade {http.request.header.Upgrade}
            header_up Connection {http.request.header.Connection}
        }
    }
    
    handle /health {
        respond "OK" 200
    }
    
    redir / /grafana 302
}
EOF
    
    log_success "Created Caddyfile"
    
    # Copy monitoring configs from project
    if [[ -d "$PROJECT_ROOT/monitoring" ]]; then
        cp -r "$PROJECT_ROOT/monitoring"/* "$STORAGE_PATH/monitoring/" 2>/dev/null || true
    fi
    
    log_success "Configuration files created"
}

# Create docker-compose.yml
create_docker_compose() {
    log_info "Creating docker-compose.yml..."
    
    # Detect if Apple Silicon for platform specification
    PLATFORM_SPEC=""
    if [[ "$ARCH" == "arm64" ]]; then
        PLATFORM_SPEC="    platform: linux/arm64"
    fi
    
    cat > "$STORAGE_PATH/docker-compose.yml" <<EOF
version: '3.9'

services:
  livekit:
    image: livekit/livekit-server:latest
    container_name: livekit
    restart: unless-stopped
$PLATFORM_SPEC
    ports:
      - "7880:7880"
      - "7881:7881"
      - "7882:7882/udp"
      - "50000-50200:50000-50200/udp"
    volumes:
      - ./config/livekit.yaml:/etc/livekit.yaml:ro
      - ./logs/livekit:/logs
      - ./data/livekit:/data
    command: --config /etc/livekit.yaml --bind 0.0.0.0
    environment:
      - LIVEKIT_CONFIG=/etc/livekit.yaml
    networks:
      - livekit-net
    logging:
      driver: "json-file"
      options:
        max-size: "10m"
        max-file: "3"

  caddy:
    image: caddy:2-alpine
    container_name: caddy
    restart: unless-stopped
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./config/Caddyfile:/etc/caddy/Caddyfile:ro
      - ./data/caddy:/data
    networks:
      - livekit-net

  prometheus:
    image: prom/prometheus:latest
    container_name: prometheus
    restart: unless-stopped
    ports:
      - "9090:9090"
    volumes:
      - ./monitoring/prometheus/prometheus.yml:/etc/prometheus/prometheus.yml:ro
      - ./data/prometheus:/prometheus
    command:
      - '--config.file=/etc/prometheus/prometheus.yml'
      - '--storage.tsdb.path=/prometheus'
      - '--storage.tsdb.retention.time=30d'
    networks:
      - livekit-net

  grafana:
    image: grafana/grafana:latest
    container_name: grafana
    restart: unless-stopped
    ports:
      - "3000:3000"
    volumes:
      - ./monitoring/grafana:/etc/grafana/provisioning:ro
      - ./monitoring/dashboards:/var/lib/grafana/dashboards:ro
      - ./data/grafana:/var/lib/grafana
    environment:
      - GF_SECURITY_ADMIN_USER=admin
      - GF_SECURITY_ADMIN_PASSWORD=$GRAFANA_PASSWORD
      - GF_USERS_ALLOW_SIGN_UP=false
      - GF_SERVER_ROOT_URL=http://localhost:3000
    networks:
      - livekit-net

  loki:
    image: grafana/loki:latest
    container_name: loki
    restart: unless-stopped
    ports:
      - "3100:3100"
    volumes:
      - ./monitoring/loki/loki-config.yml:/etc/loki/local-config.yaml:ro
      - ./data/loki:/loki
    command: -config.file=/etc/loki/local-config.yaml
    networks:
      - livekit-net

  promtail:
    image: grafana/promtail:latest
    container_name: promtail
    restart: unless-stopped
    volumes:
      - ./logs:/var/log/livekit:ro
      - ./monitoring/loki/promtail-config.yml:/etc/promtail/config.yml:ro
    command: -config.file=/etc/promtail/config.yml
    networks:
      - livekit-net

  node-exporter:
    image: prom/node-exporter:latest
    container_name: node-exporter
    restart: unless-stopped
    ports:
      - "9100:9100"
    command:
      - '--path.rootfs=/host'
    volumes:
      - '/:/host:ro,rslave'
    networks:
      - livekit-net

networks:
  livekit-net:
    driver: bridge
EOF
    
    log_success "Created docker-compose.yml"
}

# Start services
start_services() {
    log_info "Starting LiveKit services..."
    
    cd "$STORAGE_PATH"
    
    # Pull images
    log_info "Pulling Docker images (this may take a few minutes)..."
    docker compose pull
    
    # Start services
    log_info "Starting containers..."
    docker compose up -d
    
    # Wait for services to be ready
    log_info "Waiting for services to start..."
    sleep 10
    
    # Check service health
    if docker compose ps | grep -q "Up"; then
        log_success "Services started successfully"
    else
        log_error "Some services failed to start"
        docker compose ps
        exit 1
    fi
}

# Create helper scripts
create_helper_scripts() {
    log_info "Creating helper scripts..."
    
    # Create start script
    cat > "$STORAGE_PATH/start.sh" <<'EOF'
#!/bin/bash
cd "$(dirname "$0")"
docker compose up -d
echo "✓ LiveKit services started"
docker compose ps
EOF
    chmod +x "$STORAGE_PATH/start.sh"
    
    # Create stop script
    cat > "$STORAGE_PATH/stop.sh" <<'EOF'
#!/bin/bash
cd "$(dirname "$0")"
docker compose down
echo "✓ LiveKit services stopped"
EOF
    chmod +x "$STORAGE_PATH/stop.sh"
    
    # Create status script
    cat > "$STORAGE_PATH/status.sh" <<'EOF'
#!/bin/bash
cd "$(dirname "$0")"
echo "=== Service Status ==="
docker compose ps
echo ""
echo "=== Resource Usage ==="
docker stats --no-stream
EOF
    chmod +x "$STORAGE_PATH/status.sh"
    
    # Create logs script
    cat > "$STORAGE_PATH/logs.sh" <<'EOF'
#!/bin/bash
cd "$(dirname "$0")"
if [[ -n "$1" ]]; then
    docker compose logs -f "$1"
else
    docker compose logs -f
fi
EOF
    chmod +x "$STORAGE_PATH/logs.sh"
    
    log_success "Helper scripts created"
}

# Print summary
print_summary() {
    echo ""
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    log_success "LiveKit Installation Complete!"
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo ""
    echo "📍 Installation Location: $STORAGE_PATH"
    echo "🔗 Quick Access: ~/livekit"
    echo ""
    echo "🌐 Service URLs:"
    echo "   • LiveKit Server:    http://localhost:7880"
    echo "   • Grafana Dashboard: http://localhost:3000"
    echo "   • Prometheus:        http://localhost:9090"
    echo ""
    echo "🔑 Credentials:"
    echo "   • LiveKit API Key:    $API_KEY"
    echo "   • LiveKit API Secret: $API_SECRET"
    echo "   • Grafana Username:   admin"
    echo "   • Grafana Password:   $GRAFANA_PASSWORD"
    echo ""
    echo "📝 Save these credentials in a secure location!"
    echo ""
    echo "🚀 Quick Commands:"
    echo "   • Start services:  cd ~/livekit && ./start.sh"
    echo "   • Stop services:   cd ~/livekit && ./stop.sh"
    echo "   • View status:     cd ~/livekit && ./status.sh"
    echo "   • View logs:       cd ~/livekit && ./logs.sh [service]"
    echo ""
    echo "📊 Next Steps:"
    echo "   1. Open Grafana: http://localhost:3000"
    echo "   2. Login with credentials above"
    echo "   3. Explore LiveKit dashboard"
    echo "   4. (Optional) Setup public access: $SCRIPT_DIR/setup-cloudflare-tunnel.sh"
    echo ""
    echo "📖 Documentation: see 07-mac-deployment.md"
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo ""
    
    # Save credentials to file
    cat > "$STORAGE_PATH/.credentials" <<EOF
# LiveKit Credentials - Keep Secure!
# Generated: $(date)

LIVEKIT_API_KEY=$API_KEY
LIVEKIT_API_SECRET=$API_SECRET
GRAFANA_USERNAME=admin
GRAFANA_PASSWORD=$GRAFANA_PASSWORD

# Service URLs
LIVEKIT_URL=http://localhost:7880
GRAFANA_URL=http://localhost:3000
PROMETHEUS_URL=http://localhost:9090
EOF
    chmod 600 "$STORAGE_PATH/.credentials"
    
    log_success "Credentials saved to: $STORAGE_PATH/.credentials"
}

# Main installation flow
main() {
    echo ""
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo "   LiveKit Installation Script for macOS"
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo ""
    
    # Set default storage path if not provided
    STORAGE_PATH="${STORAGE_PATH:-$DEFAULT_STORAGE_PATH}"
    
    parse_args "$@"
    detect_platform
    check_prerequisites
    install_homebrew
    install_dependencies
    setup_storage
    generate_livekit_keys
    generate_grafana_password
    create_configs
    create_docker_compose
    start_services
    create_helper_scripts
    print_summary
}

# Run main function
main "$@"
