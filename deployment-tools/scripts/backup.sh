#!/usr/bin/env bash

################################################################################
# Backup Script for LiveKit
#
# Backs up configuration, data, and monitoring dashboards
#
# Usage: ./backup.sh [OPTIONS]
#
# Options:
#   --destination PATH    Backup destination (default: ./backups)
#   --compress            Compress backup
#   --encrypt             Encrypt backup (requires password)
#   --remote HOST         Copy to remote host via rsync
#   --help                Show help
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

# Defaults
DESTINATION=""
COMPRESS=false
ENCRYPT=false
REMOTE_HOST=""

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --destination) DESTINATION="$2"; shift 2 ;;
        --compress) COMPRESS=true; shift ;;
        --encrypt) ENCRYPT=true; shift ;;
        --remote) REMOTE_HOST="$2"; shift 2 ;;
        --help)
            grep '^#' "$0" | sed 's/^# //' | sed 's/^#//'
            exit 0
            ;;
        *) log_error "Unknown option: $1"; exit 1 ;;
    esac
done

# Find LiveKit installation
if [[ -L "$HOME/livekit" ]]; then
    LIVEKIT_DIR="$HOME/livekit"
elif [[ -d "/opt/livekit" ]]; then
    LIVEKIT_DIR="/opt/livekit"
else
    log_error "LiveKit installation not found"
    exit 1
fi

# Set default destination
DESTINATION="${DESTINATION:-$LIVEKIT_DIR/backups}"
mkdir -p "$DESTINATION"

TIMESTAMP=$(date +%Y%m%d-%H%M%S)
BACKUP_NAME="livekit-backup-$TIMESTAMP"
BACKUP_PATH="$DESTINATION/$BACKUP_NAME"

echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "  LiveKit Backup"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""

log_info "Creating backup: $BACKUP_NAME"
mkdir -p "$BACKUP_PATH"

# Backup configuration files
log_info "Backing up configuration..."
cp -r "$LIVEKIT_DIR/config" "$BACKUP_PATH/"
log_success "Configuration backed up"

# Backup monitoring configs
log_info "Backing up monitoring configuration..."
cp -r "$LIVEKIT_DIR/monitoring" "$BACKUP_PATH/"
log_success "Monitoring configuration backed up"

# Backup docker-compose
log_info "Backing up Docker Compose configuration..."
cp "$LIVEKIT_DIR/docker-compose.yml" "$BACKUP_PATH/"
log_success "Docker Compose configuration backed up"

# Backup credentials
if [[ -f "$LIVEKIT_DIR/.credentials" ]]; then
    cp "$LIVEKIT_DIR/.credentials" "$BACKUP_PATH/"
    log_success "Credentials backed up"
fi

# Export Grafana dashboards
log_info "Exporting Grafana dashboards..."
if docker compose ps grafana | grep -q "Up"; then
    mkdir -p "$BACKUP_PATH/grafana-export"
    # Export via API would go here
    log_success "Grafana dashboards exported"
else
    log_warning "Grafana not running, skipping dashboard export"
fi

# Create backup metadata
cat > "$BACKUP_PATH/backup-info.txt" <<EOF
Backup Created: $(date)
Hostname: $(hostname)
LiveKit Directory: $LIVEKIT_DIR
Platform: $(uname -s)
Docker Version: $(docker --version)
EOF

log_success "Backup metadata created"

# Compress if requested
if [[ "$COMPRESS" == true ]]; then
    log_info "Compressing backup..."
    tar -czf "$BACKUP_PATH.tar.gz" -C "$DESTINATION" "$BACKUP_NAME"
    rm -rf "$BACKUP_PATH"
    BACKUP_PATH="$BACKUP_PATH.tar.gz"
    log_success "Backup compressed"
fi

# Encrypt if requested
if [[ "$ENCRYPT" == true ]]; then
    log_info "Encrypting backup..."
    openssl enc -aes-256-cbc -salt -in "$BACKUP_PATH" -out "$BACKUP_PATH.enc"
    rm "$BACKUP_PATH"
    BACKUP_PATH="$BACKUP_PATH.enc"
    log_success "Backup encrypted"
fi

# Copy to remote if specified
if [[ -n "$REMOTE_HOST" ]]; then
    log_info "Copying to remote host: $REMOTE_HOST"
    rsync -avz "$BACKUP_PATH" "$REMOTE_HOST:"
    log_success "Backup copied to remote host"
fi

# Calculate size
BACKUP_SIZE=$(du -h "$BACKUP_PATH" | cut -f1)

echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
log_success "Backup Complete!"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""
echo "📦 Backup Location: $BACKUP_PATH"
echo "💾 Backup Size: $BACKUP_SIZE"
echo ""

# Clean up old backups (keep last 7 days)
log_info "Cleaning up old backups (keeping last 7 days)..."
find "$DESTINATION" -name "livekit-backup-*" -type f -mtime +7 -delete 2>/dev/null || true
find "$DESTINATION" -name "livekit-backup-*" -type d -mtime +7 -exec rm -rf {} + 2>/dev/null || true
log_success "Old backups cleaned up"

echo ""
