#!/usr/bin/env bash

################################################################################
# Backup Script for LiveKit
#
# Captures deployment configuration and local state that is useful for restoring
# a single-node LiveKit environment.
#
# Usage: ./backup.sh [OPTIONS]
#
# Options:
#   --destination PATH    Backup destination (default: <install>/backups)
#   --compress            Compress backup into .tar.gz
#   --encrypt             Encrypt the resulting archive or directory export
#   --remote HOST         Copy the resulting artifact to a remote host with rsync
#   --help                Show help
#
################################################################################

set -euo pipefail

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

DESTINATION=""
COMPRESS=false
ENCRYPT=false
REMOTE_HOST=""

log_info() { echo -e "${BLUE}[INFO]${NC} $1"; }
log_success() { echo -e "${GREEN}[OK]${NC} $1"; }
log_warning() { echo -e "${YELLOW}[WARN]${NC} $1"; }
log_error() { echo -e "${RED}[ERROR]${NC} $1"; }

show_help() {
    grep '^#' "$0" | sed 's/^# //' | sed 's/^#//'
}

while [[ $# -gt 0 ]]; do
    case "$1" in
        --destination)
            DESTINATION="$2"
            shift 2
            ;;
        --compress)
            COMPRESS=true
            shift
            ;;
        --encrypt)
            ENCRYPT=true
            shift
            ;;
        --remote)
            REMOTE_HOST="$2"
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

if [[ -L "$HOME/livekit" ]]; then
    LIVEKIT_DIR="$HOME/livekit"
elif [[ -d "/opt/livekit" ]]; then
    LIVEKIT_DIR="/opt/livekit"
else
    log_error "LiveKit installation not found"
    exit 1
fi

DESTINATION="${DESTINATION:-$LIVEKIT_DIR/backups}"
mkdir -p "$DESTINATION"

TIMESTAMP="$(date +%Y%m%d-%H%M%S)"
BACKUP_NAME="livekit-backup-$TIMESTAMP"
BACKUP_PATH="$DESTINATION/$BACKUP_NAME"

mkdir -p "$BACKUP_PATH"
cd "$LIVEKIT_DIR"

log_info "Creating backup $BACKUP_NAME"

cp -r config "$BACKUP_PATH/"
cp -r monitoring "$BACKUP_PATH/"
cp docker-compose.yml "$BACKUP_PATH/"

if [[ -f .env ]]; then
    cp .env "$BACKUP_PATH/"
fi

if [[ -f .credentials ]]; then
    cp .credentials "$BACKUP_PATH/"
fi

if [[ -d logs ]]; then
    mkdir -p "$BACKUP_PATH/logs"
    rsync -a --delete logs/ "$BACKUP_PATH/logs/"
fi

cat > "$BACKUP_PATH/backup-info.txt" <<EOF
created_at=$(date -u +%Y-%m-%dT%H:%M:%SZ)
hostname=$(hostname)
platform=$(uname -s)
livekit_dir=$LIVEKIT_DIR
docker_version=$(docker --version 2>/dev/null || echo unavailable)
compose_services=$(docker compose config --services 2>/dev/null | tr '\n' ' ')
EOF

if docker compose ps >/dev/null 2>&1; then
    docker compose ps > "$BACKUP_PATH/docker-compose-ps.txt" || true
fi

ARTIFACT_PATH="$BACKUP_PATH"

if [[ "$COMPRESS" == true ]]; then
    log_info "Compressing backup"
    tar -czf "$BACKUP_PATH.tar.gz" -C "$DESTINATION" "$BACKUP_NAME"
    rm -rf "$BACKUP_PATH"
    ARTIFACT_PATH="$BACKUP_PATH.tar.gz"
fi

if [[ "$ENCRYPT" == true ]]; then
    log_info "Encrypting backup artifact"
    openssl enc -aes-256-cbc -salt -in "$ARTIFACT_PATH" -out "$ARTIFACT_PATH.enc"
    rm -rf "$ARTIFACT_PATH"
    ARTIFACT_PATH="$ARTIFACT_PATH.enc"
fi

if [[ -n "$REMOTE_HOST" ]]; then
    log_info "Copying artifact to $REMOTE_HOST"
    rsync -avz "$ARTIFACT_PATH" "$REMOTE_HOST:"
fi

BACKUP_SIZE="$(du -sh "$ARTIFACT_PATH" | cut -f1)"

log_success "Backup complete"
echo "Artifact: $ARTIFACT_PATH"
echo "Size:     $BACKUP_SIZE"
