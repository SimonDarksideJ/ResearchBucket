#!/usr/bin/env bash

################################################################################
# LiveKit - Backup and Restore Script
# Backup configuration, credentials, and optionally data
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

BACKUP_DIR="$STORAGE_PATH/backups"
mkdir -p "$BACKUP_DIR"

show_help() {
    cat << EOF
${CYAN}LiveKit Backup and Restore${NC}

Usage: $0 [COMMAND] [OPTIONS]

Commands:
  backup              Create a backup (default)
  restore DATE        Restore from backup (e.g., 2024-01-24)
  list                List available backups
  
Options:
  --include-data      Include recordings and data (large!)
  --encrypt           Encrypt backup with password
  --help, -h          Show this help

Examples:
  $0                          # Create backup
  $0 --include-data           # Backup with data
  $0 list                     # List backups
  $0 restore 2024-01-24       # Restore specific backup
  $0 restore latest           # Restore most recent

EOF
}

# Create backup
create_backup() {
    local include_data=false
    local encrypt=false
    
    # Parse options
    while [[ $# -gt 0 ]]; do
        case $1 in
            --include-data)
                include_data=true
                shift
                ;;
            --encrypt)
                encrypt=true
                shift
                ;;
            *)
                shift
                ;;
        esac
    done
    
    local timestamp=$(date +%Y%m%d_%H%M%S)
    local backup_name="livekit-backup-$timestamp"
    local backup_file="$BACKUP_DIR/$backup_name.tar.gz"
    
    echo -e "${BLUE}📦 Creating backup: $backup_name${NC}"
    
    # Create temporary directory
    local temp_dir=$(mktemp -d)
    local backup_temp="$temp_dir/$backup_name"
    mkdir -p "$backup_temp"
    
    # Backup configuration files
    echo -e "${CYAN}Backing up configuration...${NC}"
    cp -r "$STORAGE_PATH/config" "$backup_temp/" 2>/dev/null || true
    cp "$STORAGE_PATH/docker-compose.yml" "$backup_temp/" 2>/dev/null || true
    cp "$STORAGE_PATH/.env" "$backup_temp/" 2>/dev/null || true
    cp "$STORAGE_PATH/caddy/Caddyfile" "$backup_temp/" 2>/dev/null || true
    
    # Backup monitoring configs
    echo -e "${CYAN}Backing up monitoring configuration...${NC}"
    cp -r "$STORAGE_PATH/monitoring/prometheus/prometheus.yml" "$backup_temp/" 2>/dev/null || true
    cp -r "$SCRIPT_DIR/../config" "$backup_temp/monitoring-config" 2>/dev/null || true
    
    # Backup Grafana dashboards
    if [ -d "$STORAGE_PATH/monitoring/grafana" ]; then
        echo -e "${CYAN}Backing up Grafana dashboards...${NC}"
        mkdir -p "$backup_temp/grafana"
        docker exec livekit-grafana grafana-cli admin reset-admin-password admin 2>/dev/null || true
        # Export dashboards if Grafana is running
        if docker ps | grep -q "livekit-grafana"; then
            # Note: Would need Grafana API call here
            echo -e "${YELLOW}⚠️  Manual export of Grafana dashboards recommended${NC}"
        fi
    fi
    
    # Include data if requested
    if [ "$include_data" = true ]; then
        echo -e "${CYAN}Backing up data and recordings (this may take a while)...${NC}"
        cp -r "$STORAGE_PATH/data" "$backup_temp/" 2>/dev/null || true
        cp -r "$STORAGE_PATH/recordings" "$backup_temp/" 2>/dev/null || true
    fi
    
    # Create manifest
    cat > "$backup_temp/MANIFEST.txt" << EOF
LiveKit Backup
Created: $(date)
Hostname: $(hostname)
Platform: $(uname -s)
Storage Path: $STORAGE_PATH
Include Data: $include_data

Contents:
$(ls -la "$backup_temp")
EOF
    
    # Create tarball
    echo -e "${CYAN}Creating archive...${NC}"
    tar -czf "$backup_file" -C "$temp_dir" "$backup_name"
    
    # Encrypt if requested
    if [ "$encrypt" = true ]; then
        echo -e "${CYAN}Encrypting backup...${NC}"
        read -s -p "Enter encryption password: " password
        echo ""
        echo "$password" | openssl enc -aes-256-cbc -salt -pbkdf2 -in "$backup_file" -out "${backup_file}.enc" -pass stdin
        rm "$backup_file"
        backup_file="${backup_file}.enc"
    fi
    
    # Cleanup temp
    rm -rf "$temp_dir"
    
    local size=$(du -h "$backup_file" | cut -f1)
    echo -e "${GREEN}✅ Backup created: $backup_file (${size})${NC}"
    
    # Keep only last 10 backups
    echo -e "${CYAN}Cleaning old backups...${NC}"
    ls -t "$BACKUP_DIR"/livekit-backup-*.tar.gz* 2>/dev/null | tail -n +11 | xargs rm -f 2>/dev/null || true
    echo -e "${GREEN}✅ Backup complete${NC}"
}

# List backups
list_backups() {
    echo -e "${CYAN}📋 Available Backups:${NC}"
    echo ""
    
    if ! ls "$BACKUP_DIR"/livekit-backup-*.tar.gz* 1>/dev/null 2>&1; then
        echo -e "${YELLOW}No backups found${NC}"
        return
    fi
    
    echo -e "${BLUE}Date/Time          Size      File${NC}"
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    
    for backup in $(ls -t "$BACKUP_DIR"/livekit-backup-*.tar.gz* 2>/dev/null); do
        local filename=$(basename "$backup")
        local date_str=$(echo "$filename" | sed 's/livekit-backup-//' | sed 's/\.tar\.gz.*//' | sed 's/_/ /')
        local size=$(du -h "$backup" | cut -f1)
        local encrypted=""
        
        if [[ "$filename" == *.enc ]]; then
            encrypted=" 🔒"
        fi
        
        echo -e "${date_str}  ${size}\t${filename}${encrypted}"
    done
    echo ""
}

# Restore backup
restore_backup() {
    local backup_date=$1
    
    if [ -z "$backup_date" ]; then
        echo -e "${RED}❌ Please specify backup date or 'latest'${NC}"
        list_backups
        exit 1
    fi
    
    local backup_file=""
    
    if [ "$backup_date" = "latest" ]; then
        backup_file=$(ls -t "$BACKUP_DIR"/livekit-backup-*.tar.gz* 2>/dev/null | head -1)
    else
        # Try to find backup with date
        backup_file=$(ls "$BACKUP_DIR"/livekit-backup-${backup_date}*.tar.gz* 2>/dev/null | head -1)
    fi
    
    if [ -z "$backup_file" ] || [ ! -f "$backup_file" ]; then
        echo -e "${RED}❌ Backup not found: $backup_date${NC}"
        list_backups
        exit 1
    fi
    
    echo -e "${YELLOW}⚠️  WARNING: This will overwrite current configuration!${NC}"
    read -p "Continue? (yes/no): " confirm
    
    if [ "$confirm" != "yes" ]; then
        echo -e "${BLUE}Restore cancelled${NC}"
        exit 0
    fi
    
    echo -e "${BLUE}📥 Restoring from: $(basename $backup_file)${NC}"
    
    # Stop services
    echo -e "${CYAN}Stopping services...${NC}"
    cd "$STORAGE_PATH"
    docker compose down 2>/dev/null || true
    
    # Decrypt if encrypted
    if [[ "$backup_file" == *.enc ]]; then
        echo -e "${CYAN}Decrypting backup...${NC}"
        read -s -p "Enter decryption password: " password
        echo ""
        local decrypted_file="${backup_file%.enc}"
        echo "$password" | openssl enc -aes-256-cbc -d -pbkdf2 -in "$backup_file" -out "$decrypted_file" -pass stdin
        backup_file="$decrypted_file"
    fi
    
    # Extract backup
    echo -e "${CYAN}Extracting backup...${NC}"
    local temp_dir=$(mktemp -d)
    tar -xzf "$backup_file" -C "$temp_dir"
    
    # Find backup directory
    local backup_dir=$(ls -d "$temp_dir"/livekit-backup-* | head -1)
    
    if [ ! -d "$backup_dir" ]; then
        echo -e "${RED}❌ Invalid backup format${NC}"
        rm -rf "$temp_dir"
        exit 1
    fi
    
    # Restore files
    echo -e "${CYAN}Restoring configuration files...${NC}"
    [ -d "$backup_dir/config" ] && cp -r "$backup_dir/config/"* "$STORAGE_PATH/config/" 2>/dev/null || true
    [ -f "$backup_dir/docker-compose.yml" ] && cp "$backup_dir/docker-compose.yml" "$STORAGE_PATH/" 2>/dev/null || true
    [ -f "$backup_dir/.env" ] && cp "$backup_dir/.env" "$STORAGE_PATH/" 2>/dev/null || true
    [ -f "$backup_dir/Caddyfile" ] && cp "$backup_dir/Caddyfile" "$STORAGE_PATH/caddy/" 2>/dev/null || true
    
    # Restore monitoring configs
    if [ -d "$backup_dir/monitoring-config" ]; then
        echo -e "${CYAN}Restoring monitoring configuration...${NC}"
        cp -r "$backup_dir/monitoring-config/"* "$SCRIPT_DIR/../config/" 2>/dev/null || true
    fi
    
    # Cleanup
    rm -rf "$temp_dir"
    
    # Start services
    echo -e "${CYAN}Starting services...${NC}"
    docker compose up -d
    
    sleep 5
    
    if docker compose ps | grep -q "Up"; then
        echo -e "${GREEN}✅ Restore complete${NC}"
        echo -e "${CYAN}ℹ️  Please verify your configuration and credentials${NC}"
    else
        echo -e "${RED}❌ Services failed to start after restore${NC}"
        echo -e "${CYAN}Check logs: ./scripts/logs.sh${NC}"
    fi
}

# Main
case "${1:-backup}" in
    backup)
        shift
        create_backup "$@"
        ;;
    restore)
        restore_backup "$2"
        ;;
    list)
        list_backups
        ;;
    --help|-h)
        show_help
        ;;
    *)
        echo -e "${RED}Unknown command: $1${NC}"
        show_help
        exit 1
        ;;
esac
