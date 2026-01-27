#!/usr/bin/env bash

################################################################################
# LiveKit Mac Deployment - Main Setup Script
# 
# This script provides an interactive setup for LiveKit on macOS with:
# - External storage configuration
# - Docker Compose deployment
# - Monitoring (Grafana + Prometheus)
# - Reverse proxy options
# - Cross-platform compatibility (Mac/Linux)
#
# Usage: ./setup.sh [--unattended] [--storage-path PATH]
################################################################################

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
MAGENTA='\033[0;35m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Script directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../../.." && pwd)"

# Default values
UNATTENDED=false
STORAGE_PATH=""
INSTALL_MONITORING=true
REVERSE_PROXY_TYPE="ngrok"

# Detect platform
if [[ "$OSTYPE" == "darwin"* ]]; then
    PLATFORM="mac"
    STORAGE_DEFAULT="/Volumes/LiveKitStorage"
    PKG_MANAGER="brew"
elif [[ "$OSTYPE" == "linux-gnu"* ]]; then
    PLATFORM="linux"
    STORAGE_DEFAULT="/opt/livekit-storage"
    PKG_MANAGER="apt"
else
    echo -e "${RED}❌ Unsupported operating system: $OSTYPE${NC}"
    exit 1
fi

################################################################################
# Helper Functions
################################################################################

print_header() {
    echo ""
    echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo -e "${CYAN}$1${NC}"
    echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
}

print_step() {
    echo ""
    echo -e "${BLUE}📋 $1${NC}"
}

print_success() {
    echo -e "${GREEN}✅ $1${NC}"
}

print_error() {
    echo -e "${RED}❌ $1${NC}"
}

print_warning() {
    echo -e "${YELLOW}⚠️  $1${NC}"
}

print_info() {
    echo -e "${CYAN}ℹ️  $1${NC}"
}

# Parse command line arguments
parse_args() {
    while [[ $# -gt 0 ]]; do
        case $1 in
            --unattended)
                UNATTENDED=true
                shift
                ;;
            --storage-path)
                STORAGE_PATH="$2"
                shift 2
                ;;
            --no-monitoring)
                INSTALL_MONITORING=false
                shift
                ;;
            --proxy)
                REVERSE_PROXY_TYPE="$2"
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
}

show_help() {
    cat << EOF
LiveKit Mac Deployment Setup Script

Usage: $0 [OPTIONS]

Options:
    --unattended            Run in non-interactive mode
    --storage-path PATH     Set external storage path
    --no-monitoring         Skip monitoring stack installation
    --proxy TYPE            Set reverse proxy type (ngrok|cloudflare|tailscale|localtunnel|none)
    -h, --help              Show this help message

Examples:
    $0                                          # Interactive setup
    $0 --unattended --storage-path /Volumes/MyDrive
    $0 --proxy cloudflare --no-monitoring

EOF
}

# Check if command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Check system requirements
check_requirements() {
    print_step "Step 1: Checking System Requirements"
    
    local missing_deps=()
    
    # Check Docker
    if command_exists docker; then
        print_success "Docker installed: $(docker --version | cut -d' ' -f3 | tr -d ',')"
    else
        print_error "Docker not installed"
        missing_deps+=("docker")
    fi
    
    # Check Docker Compose
    if docker compose version >/dev/null 2>&1; then
        print_success "Docker Compose installed: $(docker compose version --short)"
    else
        print_error "Docker Compose not installed"
        missing_deps+=("docker-compose")
    fi
    
    # Check platform-specific tools
    if [[ $PLATFORM == "mac" ]]; then
        if command_exists brew; then
            print_success "Homebrew installed"
        else
            print_warning "Homebrew not installed (recommended)"
            missing_deps+=("homebrew")
        fi
    fi
    
    # Check required tools
    for tool in curl jq openssl; do
        if command_exists $tool; then
            print_success "$tool installed"
        else
            print_warning "$tool not installed (will attempt to install)"
            missing_deps+=("$tool")
        fi
    done
    
    # Install missing dependencies
    if [ ${#missing_deps[@]} -gt 0 ]; then
        echo ""
        print_warning "Missing dependencies: ${missing_deps[*]}"
        
        if [[ $UNATTENDED == false ]]; then
            read -p "Attempt to install missing dependencies? (y/n): " -n 1 -r
            echo
            if [[ $REPLY =~ ^[Yy]$ ]]; then
                install_dependencies "${missing_deps[@]}"
            else
                print_error "Cannot continue without required dependencies"
                exit 1
            fi
        else
            install_dependencies "${missing_deps[@]}"
        fi
    fi
    
    # Check Docker daemon
    if ! docker ps >/dev/null 2>&1; then
        print_error "Docker daemon not running. Please start Docker Desktop and try again."
        exit 1
    fi
    
    print_success "All requirements satisfied"
}

# Install missing dependencies
install_dependencies() {
    local deps=("$@")
    print_step "Installing missing dependencies..."
    
    for dep in "${deps[@]}"; do
        case $dep in
            docker)
                if [[ $PLATFORM == "mac" ]]; then
                    print_error "Please install Docker Desktop from: https://www.docker.com/products/docker-desktop"
                    exit 1
                else
                    # Install Docker on Linux
                    curl -fsSL https://get.docker.com -o /tmp/get-docker.sh
                    sh /tmp/get-docker.sh
                    usermod -aG docker $USER
                fi
                ;;
            homebrew)
                /bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
                ;;
            jq|curl|openssl)
                if [[ $PLATFORM == "mac" ]]; then
                    brew install $dep
                else
                    apt-get update && apt-get install -y $dep
                fi
                ;;
        esac
    done
}

# Detect and select storage
configure_storage() {
    print_step "Step 2: Storage Configuration"
    
    if [[ -n "$STORAGE_PATH" ]]; then
        print_info "Using storage path: $STORAGE_PATH"
    else
        if [[ $PLATFORM == "mac" ]]; then
            # List available volumes
            echo ""
            echo "Available external drives:"
            local volumes=($(ls -1 /Volumes/ 2>/dev/null | grep -v "Macintosh HD"))
            
            if [ ${#volumes[@]} -eq 0 ]; then
                print_warning "No external drives detected"
                print_info "Using default path: $STORAGE_DEFAULT"
                STORAGE_PATH="$STORAGE_DEFAULT"
            else
                local idx=1
                for vol in "${volumes[@]}"; do
                    local vol_path="/Volumes/$vol"
                    local size=$(df -h "$vol_path" 2>/dev/null | awk 'NR==2 {print $4}')
                    echo "  $idx) $vol_path (${size} free)"
                    ((idx++))
                done
                echo "  $idx) Enter custom path"
                echo ""
                
                if [[ $UNATTENDED == false ]]; then
                    read -p "Select external storage [1-$idx]: " choice
                    
                    if [[ $choice -eq $idx ]]; then
                        read -p "Enter storage path: " STORAGE_PATH
                    elif [[ $choice -ge 1 && $choice -lt $idx ]]; then
                        STORAGE_PATH="/Volumes/${volumes[$((choice-1))]}/livekit"
                    else
                        print_error "Invalid selection"
                        exit 1
                    fi
                else
                    # In unattended mode, use first available or default
                    STORAGE_PATH="/Volumes/${volumes[0]}/livekit"
                fi
            fi
        else
            # Linux default
            STORAGE_PATH="$STORAGE_DEFAULT"
        fi
    fi
    
    # Create directory structure
    print_info "Creating directory structure at: $STORAGE_PATH"
    mkdir -p "$STORAGE_PATH"/{data,logs,config,recordings,backups}
    mkdir -p "$STORAGE_PATH"/monitoring/{prometheus,grafana,loki}
    mkdir -p "$STORAGE_PATH"/caddy/{data,config}
    
    # Test write access
    if touch "$STORAGE_PATH"/.test 2>/dev/null; then
        rm "$STORAGE_PATH"/.test
        print_success "Storage is writable"
    else
        print_error "Cannot write to storage path: $STORAGE_PATH"
        exit 1
    fi
    
    # Check available space
    local available=$(df -h "$STORAGE_PATH" | awk 'NR==2 {print $4}')
    print_success "Available space: $available"
}

# Generate API credentials
generate_credentials() {
    print_step "Step 3: Generating API Credentials"
    
    API_KEY=$(openssl rand -hex 16)
    API_SECRET=$(openssl rand -base64 32)
    GRAFANA_PASSWORD=$(openssl rand -base64 16 | tr -d '=+/' | cut -c1-16)
    
    print_success "API_KEY: $API_KEY"
    print_info "API_SECRET: [hidden - saved to .env]"
    print_info "Grafana Password: $GRAFANA_PASSWORD"
    
    # Save to .env file
    cat > "$STORAGE_PATH"/.env << EOF
# LiveKit Configuration
LIVEKIT_DOMAIN=localhost
LIVEKIT_API_KEY=$API_KEY
LIVEKIT_API_SECRET=$API_SECRET

# Grafana Configuration
GF_SECURITY_ADMIN_PASSWORD=$GRAFANA_PASSWORD

# Storage Path
STORAGE_PATH=$STORAGE_PATH
EOF
    
    chmod 600 "$STORAGE_PATH"/.env
    print_success "Credentials saved to $STORAGE_PATH/.env"
}

# Create LiveKit configuration
create_livekit_config() {
    print_step "Step 4: Creating LiveKit Configuration"
    
    cat > "$STORAGE_PATH"/config/config.yaml << 'EOF'
port: 7880
bind_addresses:
  - "0.0.0.0"

rtc:
  port_range_start: 50000
  port_range_end: 60000
  use_ice_lite: true
  # Uncomment for TCP fallback
  # tcp_port: 7881
  # udp_port: 7882

region: local

# Keys injected via LIVEKIT_KEYS environment variable

logging:
  level: info
  json: true

room:
  auto_create: true
  empty_timeout: 300
  max_participants: 50
  departure_timeout: 20

# Optional: Enable recording
# recording:
#   enabled: true
#   storage:
#     type: filesystem
#     path: /recordings
EOF
    
    print_success "LiveKit configuration created"
}

# Create Docker Compose stack
create_docker_compose() {
    print_step "Step 5: Creating Docker Compose Stack"
    
    cat > "$STORAGE_PATH"/docker-compose.yml << EOF
version: "3.9"

services:
  livekit:
    image: livekit/livekit-server:latest
    container_name: livekit
    restart: unless-stopped
    
    ports:
      - "127.0.0.1:7880:7880/tcp"
      - "50000-60000:50000-60000/udp"
    
    volumes:
      - $STORAGE_PATH/config:/config:ro
      - $STORAGE_PATH/data:/data
      - $STORAGE_PATH/recordings:/recordings
      - $STORAGE_PATH/logs:/logs
    
    environment:
      - LIVEKIT_KEYS=\${LIVEKIT_API_KEY}:\${LIVEKIT_API_SECRET}
    
    command: ["--config", "/config/config.yaml"]
    
    logging:
      driver: "json-file"
      options:
        max-size: "100m"
        max-file: "10"
        labels: "service=livekit"
    
    healthcheck:
      test: ["CMD", "wget", "--quiet", "--tries=1", "--spider", "http://localhost:7880/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s

  caddy:
    image: caddy:2
    container_name: livekit-caddy
    restart: unless-stopped
    
    ports:
      - "8080:80"
      - "8443:443"
    
    volumes:
      - $STORAGE_PATH/caddy/Caddyfile:/etc/caddy/Caddyfile:ro
      - $STORAGE_PATH/caddy/data:/data
      - $STORAGE_PATH/caddy/config:/config
    
    depends_on:
      - livekit
    
    environment:
      - LIVEKIT_DOMAIN=\${LIVEKIT_DOMAIN:-localhost}

EOF

    # Add monitoring services if enabled
    if [[ $INSTALL_MONITORING == true ]]; then
        cat >> "$STORAGE_PATH"/docker-compose.yml << EOF
  prometheus:
    image: prom/prometheus:latest
    container_name: livekit-prometheus
    restart: unless-stopped
    
    ports:
      - "127.0.0.1:9090:9090"
    
    volumes:
      - $STORAGE_PATH/monitoring/prometheus/prometheus.yml:/etc/prometheus/prometheus.yml:ro
      - $STORAGE_PATH/monitoring/prometheus/data:/prometheus
    
    command:
      - '--config.file=/etc/prometheus/prometheus.yml'
      - '--storage.tsdb.path=/prometheus'
      - '--web.console.libraries=/etc/prometheus/console_libraries'
      - '--web.console.templates=/etc/prometheus/consoles'
      - '--storage.tsdb.retention.time=30d'
    
    depends_on:
      - livekit

  grafana:
    image: grafana/grafana:latest
    container_name: livekit-grafana
    restart: unless-stopped
    
    ports:
      - "3000:3000"
    
    volumes:
      - $STORAGE_PATH/monitoring/grafana:/var/lib/grafana
      - $SCRIPT_DIR/../config/grafana-dashboards:/etc/grafana/provisioning/dashboards:ro
      - $SCRIPT_DIR/../config/grafana-datasources.yml:/etc/grafana/provisioning/datasources/datasources.yml:ro
    
    environment:
      - GF_SECURITY_ADMIN_USER=admin
      - GF_SECURITY_ADMIN_PASSWORD=\${GF_SECURITY_ADMIN_PASSWORD}
      - GF_USERS_ALLOW_SIGN_UP=false
    
    depends_on:
      - prometheus

  loki:
    image: grafana/loki:latest
    container_name: livekit-loki
    restart: unless-stopped
    
    ports:
      - "127.0.0.1:3100:3100"
    
    volumes:
      - $STORAGE_PATH/monitoring/loki:/loki
      - $SCRIPT_DIR/../config/loki-config.yml:/etc/loki/local-config.yaml:ro
    
    command: -config.file=/etc/loki/local-config.yaml

  promtail:
    image: grafana/promtail:latest
    container_name: livekit-promtail
    restart: unless-stopped
    
    volumes:
      - $STORAGE_PATH/logs:/var/log/livekit:ro
      - /var/lib/docker/containers:/var/lib/docker/containers:ro
      - $SCRIPT_DIR/../config/promtail-config.yml:/etc/promtail/config.yml:ro
    
    command: -config.file=/etc/promtail/config.yml
    
    depends_on:
      - loki

EOF
    fi
    
    # Add networks section
    cat >> "$STORAGE_PATH"/docker-compose.yml << EOF
networks:
  default:
    name: livekit-network
    driver: bridge
EOF
    
    print_success "Docker Compose configuration created"
}

# Create Caddy configuration
create_caddy_config() {
    cat > "$STORAGE_PATH"/caddy/Caddyfile << 'EOF'
{
    # Global options
    admin off
    auto_https off
}

:80 {
    encode gzip
    
    # Health check endpoint
    handle /health {
        reverse_proxy livekit:7880
    }
    
    # All other traffic
    handle {
        reverse_proxy livekit:7880
    }
}
EOF
    
    print_success "Caddy configuration created"
}

# Configure monitoring
configure_monitoring() {
    if [[ $INSTALL_MONITORING == false ]]; then
        return
    fi
    
    print_step "Step 6: Configuring Monitoring Stack"
    
    # Prometheus configuration
    cat > "$STORAGE_PATH"/monitoring/prometheus/prometheus.yml << EOF
global:
  scrape_interval: 15s
  evaluation_interval: 15s

scrape_configs:
  - job_name: 'livekit'
    static_configs:
      - targets: ['livekit:7880']
    metrics_path: '/metrics'
  
  - job_name: 'prometheus'
    static_configs:
      - targets: ['localhost:9090']
  
  - job_name: 'caddy'
    static_configs:
      - targets: ['caddy:2019']
EOF
    
    # Grafana datasources
    mkdir -p "$SCRIPT_DIR"/../config
    cat > "$SCRIPT_DIR"/../config/grafana-datasources.yml << EOF
apiVersion: 1

datasources:
  - name: Prometheus
    type: prometheus
    access: proxy
    url: http://prometheus:9090
    isDefault: true
    editable: true
  
  - name: Loki
    type: loki
    access: proxy
    url: http://loki:3100
    editable: true
EOF
    
    # Loki configuration
    cat > "$SCRIPT_DIR"/../config/loki-config.yml << EOF
auth_enabled: false

server:
  http_listen_port: 3100

ingester:
  lifecycler:
    address: 127.0.0.1
    ring:
      kvstore:
        store: inmemory
      replication_factor: 1
    final_sleep: 0s
  chunk_idle_period: 5m
  chunk_retain_period: 30s

schema_config:
  configs:
    - from: 2020-10-24
      store: boltdb
      object_store: filesystem
      schema: v11
      index:
        prefix: index_
        period: 168h

storage_config:
  boltdb:
    directory: /loki/index
  filesystem:
    directory: /loki/chunks

limits_config:
  enforce_metric_name: false
  reject_old_samples: true
  reject_old_samples_max_age: 168h

chunk_store_config:
  max_look_back_period: 0s

table_manager:
  retention_deletes_enabled: false
  retention_period: 0s
EOF
    
    # Promtail configuration
    cat > "$SCRIPT_DIR"/../config/promtail-config.yml << EOF
server:
  http_listen_port: 9080
  grpc_listen_port: 0

positions:
  filename: /tmp/positions.yaml

clients:
  - url: http://loki:3100/loki/api/v1/push

scrape_configs:
  - job_name: livekit
    static_configs:
      - targets:
          - localhost
        labels:
          job: livekit
          __path__: /var/log/livekit/*.log
  
  - job_name: docker
    static_configs:
      - targets:
          - localhost
        labels:
          job: docker
          __path__: /var/lib/docker/containers/*/*-json.log
EOF
    
    print_success "Monitoring configuration created"
}

# Setup reverse proxy
setup_reverse_proxy() {
    print_step "Step 7: Reverse Proxy Setup"
    
    if [[ $UNATTENDED == false ]]; then
        echo ""
        echo "How do you want to access LiveKit externally?"
        echo ""
        echo "  1) ngrok (easiest, requires free account)"
        echo "  2) Cloudflare Tunnel (free, requires domain)"
        echo "  3) Tailscale (private network, free)"
        echo "  4) LocalTunnel (free, random URLs)"
        echo "  5) Skip (local network only)"
        echo ""
        read -p "Select option [1-5]: " proxy_choice
        
        case $proxy_choice in
            1) REVERSE_PROXY_TYPE="ngrok" ;;
            2) REVERSE_PROXY_TYPE="cloudflare" ;;
            3) REVERSE_PROXY_TYPE="tailscale" ;;
            4) REVERSE_PROXY_TYPE="localtunnel" ;;
            5) REVERSE_PROXY_TYPE="none" ;;
            *) print_warning "Invalid choice, skipping reverse proxy"; REVERSE_PROXY_TYPE="none" ;;
        esac
    fi
    
    case $REVERSE_PROXY_TYPE in
        ngrok)
            setup_ngrok
            ;;
        cloudflare)
            setup_cloudflare_tunnel
            ;;
        tailscale)
            setup_tailscale
            ;;
        localtunnel)
            setup_localtunnel
            ;;
        none)
            print_info "Skipping reverse proxy setup"
            print_info "LiveKit will only be accessible on: http://localhost:7880"
            ;;
    esac
}

setup_ngrok() {
    print_info "Setting up ngrok tunnel..."
    
    if ! command_exists ngrok; then
        if [[ $PLATFORM == "mac" ]]; then
            brew install ngrok
        else
            print_warning "Please install ngrok manually: https://ngrok.com/download"
            return
        fi
    fi
    
    print_warning "ngrok requires an auth token"
    print_info "1. Sign up at https://dashboard.ngrok.com/signup"
    print_info "2. Get your auth token from https://dashboard.ngrok.com/get-started/your-authtoken"
    echo ""
    
    if [[ $UNATTENDED == false ]]; then
        read -p "Enter ngrok auth token (or press Enter to skip): " ngrok_token
        
        if [[ -n "$ngrok_token" ]]; then
            ngrok config add-authtoken "$ngrok_token"
            print_success "ngrok configured"
            
            # Create ngrok configuration for background running
            cat > "$STORAGE_PATH"/config/ngrok.yml << EOF
version: "2"
authtoken: $ngrok_token
tunnels:
  livekit:
    proto: http
    addr: localhost:7880
EOF
            
            print_success "ngrok tunnel will start with services"
        else
            print_info "Skipping ngrok setup"
        fi
    fi
}

setup_cloudflare_tunnel() {
    print_info "Setting up Cloudflare Tunnel..."
    print_warning "Cloudflare Tunnel requires:"
    print_info "1. A Cloudflare account"
    print_info "2. A domain managed by Cloudflare"
    print_info "3. cloudflared CLI installed"
    echo ""
    print_info "Please follow the setup guide in the documentation"
    print_info "Or run: brew install cloudflare/cloudflare/cloudflared"
}

setup_tailscale() {
    print_info "Setting up Tailscale..."
    
    if ! command_exists tailscale; then
        if [[ $PLATFORM == "mac" ]]; then
            print_info "Installing Tailscale..."
            brew install tailscale
        else
            print_info "Please install Tailscale: https://tailscale.com/download"
            return
        fi
    fi
    
    print_info "Run: sudo tailscale up"
    print_info "Then access via your Tailscale IP: tailscale ip -4"
}

setup_localtunnel() {
    print_info "Setting up LocalTunnel..."
    
    if ! command_exists lt; then
        print_warning "LocalTunnel requires Node.js"
        print_info "Install with: npm install -g localtunnel"
        print_info "Then run: lt --port 7880"
    fi
}

# Start services
start_services() {
    print_step "Step 8: Starting Services"
    
    cd "$STORAGE_PATH"
    
    print_info "Pulling Docker images..."
    docker compose pull
    
    print_info "Starting containers..."
    docker compose up -d
    
    print_info "Waiting for services to be ready..."
    sleep 10
    
    # Check health
    if docker compose ps | grep -q "Up"; then
        print_success "Services started successfully"
    else
        print_error "Some services failed to start"
        docker compose ps
        exit 1
    fi
}

# Verify installation
verify_installation() {
    print_step "Step 9: Verifying Installation"
    
    # Check LiveKit health
    if curl -f -s http://localhost:7880/health > /dev/null 2>&1; then
        print_success "LiveKit is responding"
    else
        print_error "LiveKit health check failed"
    fi
    
    # Check Prometheus
    if [[ $INSTALL_MONITORING == true ]]; then
        if curl -f -s http://localhost:9090/-/healthy > /dev/null 2>&1; then
            print_success "Prometheus is responding"
        else
            print_warning "Prometheus health check failed"
        fi
        
        # Check Grafana
        if curl -f -s http://localhost:3000/api/health > /dev/null 2>&1; then
            print_success "Grafana is responding"
        else
            print_warning "Grafana health check failed"
        fi
    fi
}

# Display summary
show_summary() {
    print_header "🎉 Setup Complete!"
    
    echo ""
    echo -e "${CYAN}📡 Access Points:${NC}"
    echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo -e "LiveKit Server (local):  ${GREEN}http://localhost:7880${NC}"
    
    if [[ $REVERSE_PROXY_TYPE == "ngrok" ]]; then
        local ngrok_url=$(curl -s http://127.0.0.1:4040/api/tunnels 2>/dev/null | jq -r '.tunnels[0].public_url' 2>/dev/null || echo "Not available")
        echo -e "LiveKit Server (public): ${GREEN}${ngrok_url}${NC}"
    fi
    
    if [[ $INSTALL_MONITORING == true ]]; then
        echo -e "Monitoring Dashboard:    ${GREEN}http://localhost:3000${NC}"
        echo -e "  Username: ${YELLOW}admin${NC}"
        echo -e "  Password: ${YELLOW}${GRAFANA_PASSWORD}${NC}"
    fi
    
    echo ""
    echo -e "${CYAN}🔑 API Credentials:${NC}"
    echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo -e "API_KEY:    ${YELLOW}${API_KEY}${NC}"
    echo -e "API_SECRET: ${YELLOW}[saved in .env file]${NC}"
    
    echo ""
    echo -e "${CYAN}📁 Data Location:${NC}"
    echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo -e "All data stored at: ${GREEN}${STORAGE_PATH}${NC}"
    
    echo ""
    echo -e "${CYAN}💡 Next Steps:${NC}"
    echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    if [[ $INSTALL_MONITORING == true ]]; then
        echo -e "1. Open monitoring dashboard: ${GREEN}open http://localhost:3000${NC}"
    fi
    echo -e "2. Test LiveKit health: ${GREEN}curl http://localhost:7880/health${NC}"
    echo -e "3. View logs: ${GREEN}$SCRIPT_DIR/logs.sh${NC}"
    echo -e "4. Check status: ${GREEN}$SCRIPT_DIR/status.sh${NC}"
    
    echo ""
    echo -e "${CYAN}📖 Documentation: ${GREEN}docs/livekit-deployment/08-mac-deployment.md${NC}"
    echo ""
}

################################################################################
# Main Execution
################################################################################

main() {
    # Parse arguments
    parse_args "$@"
    
    # Print banner
    print_header "🚀 LiveKit Mac Deployment Setup"
    
    echo ""
    echo -e "${BLUE}Platform:${NC} $PLATFORM"
    echo -e "${BLUE}Storage:${NC} ${STORAGE_PATH:-auto-detect}"
    echo -e "${BLUE}Monitoring:${NC} ${INSTALL_MONITORING}"
    echo -e "${BLUE}Proxy:${NC} ${REVERSE_PROXY_TYPE}"
    
    # Execute setup steps
    check_requirements
    configure_storage
    generate_credentials
    create_livekit_config
    create_docker_compose
    create_caddy_config
    configure_monitoring
    setup_reverse_proxy
    start_services
    verify_installation
    show_summary
}

# Run main function
main "$@"
