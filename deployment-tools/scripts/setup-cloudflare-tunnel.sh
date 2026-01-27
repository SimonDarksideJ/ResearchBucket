#!/usr/bin/env bash

################################################################################
# Cloudflare Tunnel Setup Script
#
# Configures Cloudflare Tunnel for public access to LiveKit without
# opening ports on your router
#
# Usage: ./setup-cloudflare-tunnel.sh [OPTIONS]
#
# Options:
#   --domain DOMAIN    Use custom domain (requires Cloudflare DNS)
#   --help             Show help
#
################################################################################

set -e

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

log_info() { echo -e "${BLUE}ℹ${NC} $1"; }
log_success() { echo -e "${GREEN}✓${NC} $1"; }
log_warning() { echo -e "${YELLOW}⚠${NC} $1"; }
log_error() { echo -e "${RED}✗${NC} $1"; }

CUSTOM_DOMAIN=""

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --domain)
            CUSTOM_DOMAIN="$2"
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

# Check if cloudflared is installed
if ! command -v cloudflared &> /dev/null; then
    log_error "cloudflared is not installed"
    log_info "Install with: brew install cloudflared"
    exit 1
fi

# Find LiveKit installation
if [[ -L "$HOME/livekit" ]]; then
    LIVEKIT_DIR="$HOME/livekit"
elif [[ -d "/opt/livekit" ]]; then
    LIVEKIT_DIR="/opt/livekit"
else
    log_error "LiveKit installation not found"
    exit 1
fi

cd "$LIVEKIT_DIR"

echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "  Cloudflare Tunnel Setup"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""

log_info "This will set up secure public access to your LiveKit server"
log_info "No router configuration needed!"
echo ""

# Authenticate with Cloudflare
log_info "Step 1: Authenticate with Cloudflare"
log_info "A browser window will open for authentication..."
sleep 2

cloudflared tunnel login

if [[ $? -ne 0 ]]; then
    log_error "Authentication failed"
    exit 1
fi

log_success "Authenticated with Cloudflare"
echo ""

# Create tunnel
log_info "Step 2: Creating Cloudflare Tunnel..."

TUNNEL_NAME="livekit-$(hostname | tr '[:upper:]' '[:lower:]' | tr '.' '-')"
cloudflared tunnel create "$TUNNEL_NAME"

if [[ $? -ne 0 ]]; then
    log_warning "Tunnel may already exist, trying to use existing tunnel..."
fi

log_success "Tunnel created: $TUNNEL_NAME"
echo ""

# Create tunnel configuration
log_info "Step 3: Configuring tunnel routes..."

mkdir -p "$HOME/.cloudflared"

if [[ -n "$CUSTOM_DOMAIN" ]]; then
    # Custom domain configuration
    cat > "$HOME/.cloudflared/config.yml" <<EOF
tunnel: $TUNNEL_NAME
credentials-file: $HOME/.cloudflared/${TUNNEL_NAME}.json

ingress:
  - hostname: $CUSTOM_DOMAIN
    service: http://localhost:7880
  - hostname: grafana.$CUSTOM_DOMAIN
    service: http://localhost:3000
  - service: http_status:404
EOF
    
    log_info "Configuring DNS for custom domain..."
    log_info "Please add the following DNS records in Cloudflare:"
    echo ""
    echo "  $CUSTOM_DOMAIN          CNAME   $TUNNEL_NAME.cfargotunnel.com"
    echo "  grafana.$CUSTOM_DOMAIN  CNAME   $TUNNEL_NAME.cfargotunnel.com"
    echo ""
    read -p "Press Enter after adding DNS records..."
    
else
    # Quick tunnel (random subdomain)
    log_info "Using quick tunnel (random subdomain)..."
    log_warning "This URL will change if tunnel restarts"
    log_info "For permanent URL, use --domain flag with your domain"
    echo ""
fi

# Add cloudflared to docker-compose
log_info "Step 4: Adding tunnel to Docker Compose..."

if grep -q "cloudflared" docker-compose.yml; then
    log_warning "Cloudflared already in docker-compose.yml"
else
    cat >> docker-compose.yml <<EOF

  cloudflared:
    image: cloudflare/cloudflared:latest
    container_name: cloudflared
    restart: unless-stopped
    command: tunnel --no-autoupdate run --token \${CLOUDFLARE_TUNNEL_TOKEN}
    environment:
      - CLOUDFLARE_TUNNEL_TOKEN=\${CLOUDFLARE_TUNNEL_TOKEN}
    networks:
      - livekit-net
EOF
    
    log_success "Added cloudflared service to docker-compose.yml"
fi

# Start tunnel
log_info "Step 5: Starting tunnel..."

if [[ -n "$CUSTOM_DOMAIN" ]]; then
    # Start tunnel as service
    docker compose up -d cloudflared
    
    log_success "Tunnel started!"
    echo ""
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo "✅ Cloudflare Tunnel Configured!"
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo ""
    echo "🌐 Public URLs:"
    echo "   LiveKit:  https://$CUSTOM_DOMAIN"
    echo "   Grafana:  https://grafana.$CUSTOM_DOMAIN"
    echo ""
    echo "🔒 All traffic is encrypted via Cloudflare"
    echo "📊 View tunnel status: cloudflared tunnel info $TUNNEL_NAME"
    echo ""
else
    # Quick tunnel
    log_info "Starting quick tunnel (this will generate URLs)..."
    log_warning "Keep this terminal open or the tunnel will close"
    echo ""
    
    cloudflared tunnel --url http://localhost:7880 &
    TUNNEL_PID=$!
    
    sleep 5
    
    log_success "Tunnel running!"
    log_info "Check terminal output above for your public URL"
    echo ""
    echo "To stop tunnel: kill $TUNNEL_PID"
fi

echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
