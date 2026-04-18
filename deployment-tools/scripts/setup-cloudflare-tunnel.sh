#!/usr/bin/env bash

################################################################################
# Cloudflare Tunnel Helper
#
# Creates a Cloudflare Tunnel for admin or signaling endpoints. This helper is
# intentionally conservative: Cloudflare Tunnel is useful for dashboards and
# limited signaling access, but it does not replace LiveKit's required UDP media
# path.
#
# Usage: ./setup-cloudflare-tunnel.sh [OPTIONS]
#
# Options:
#   --service [grafana|livekit]   Which local service to expose (default: grafana)
#   --hostname HOSTNAME           Create a named tunnel for this hostname
#   --name NAME                   Tunnel name (default: livekit-<hostname>)
#   --help                        Show help
#
################################################################################

set -euo pipefail

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

SERVICE="grafana"
HOSTNAME=""
TUNNEL_NAME=""
TUNNEL_ID=""

log_info() { echo -e "${BLUE}[INFO]${NC} $1"; }
log_success() { echo -e "${GREEN}[OK]${NC} $1"; }
log_warning() { echo -e "${YELLOW}[WARN]${NC} $1"; }
log_error() { echo -e "${RED}[ERROR]${NC} $1"; }

show_help() {
    grep '^#' "$0" | sed 's/^# //' | sed 's/^#//'
}

while [[ $# -gt 0 ]]; do
    case "$1" in
        --service)
            SERVICE="$2"
            shift 2
            ;;
        --hostname)
            HOSTNAME="$2"
            shift 2
            ;;
        --name)
            TUNNEL_NAME="$2"
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

if ! command -v cloudflared >/dev/null 2>&1; then
    log_error "cloudflared is not installed"
    exit 1
fi

case "$SERVICE" in
    grafana)
        TARGET_URL="http://localhost:3000"
        ;;
    livekit)
        TARGET_URL="http://localhost:7880"
        log_warning "Cloudflare Tunnel can expose LiveKit signaling, but not the UDP media path or TURN/UDP ports clients still need."
        ;;
    *)
        log_error "Unsupported service: $SERVICE"
        exit 1
        ;;
esac

if [[ -z "$TUNNEL_NAME" ]]; then
    TUNNEL_NAME="livekit-$(hostname | tr '[:upper:]' '[:lower:]' | tr '.' '-')-$SERVICE"
fi

if [[ -z "$HOSTNAME" ]]; then
    log_info "Starting a quick tunnel to $TARGET_URL"
    log_warning "Quick tunnels are transient and best suited to short-lived admin access."
    cloudflared tunnel --url "$TARGET_URL"
    exit 0
fi

mkdir -p "$HOME/.cloudflared"

resolve_tunnel_id() {
    cloudflared tunnel list 2>/dev/null | awk -v name="$TUNNEL_NAME" '$2 == name {print $1; exit}'
}

log_info "Authenticating with Cloudflare"
cloudflared tunnel login

TUNNEL_ID="$(resolve_tunnel_id || true)"

if [[ -z "$TUNNEL_ID" ]]; then
    log_info "Creating tunnel $TUNNEL_NAME"
    CREATE_OUTPUT="$(cloudflared tunnel create "$TUNNEL_NAME")"
    TUNNEL_ID="$(echo "$CREATE_OUTPUT" | sed -nE 's/.* id ([0-9a-fA-F-]+).*/\1/p' | tail -1)"
fi

if [[ -z "$TUNNEL_ID" ]]; then
    log_error "Could not determine the Cloudflare tunnel ID for $TUNNEL_NAME"
    exit 1
fi

log_info "Routing hostname $HOSTNAME to tunnel $TUNNEL_NAME"
cloudflared tunnel route dns "$TUNNEL_NAME" "$HOSTNAME"

cat > "$HOME/.cloudflared/config.yml" <<EOF
tunnel: $TUNNEL_ID
credentials-file: $HOME/.cloudflared/${TUNNEL_ID}.json

ingress:
  - hostname: $HOSTNAME
    service: $TARGET_URL
  - service: http_status:404
EOF

log_success "Tunnel configuration written to $HOME/.cloudflared/config.yml"
log_info "Run 'cloudflared tunnel run $TUNNEL_NAME' to start the tunnel"
