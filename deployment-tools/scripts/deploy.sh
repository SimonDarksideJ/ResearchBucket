#!/usr/bin/env bash

################################################################################
# Universal Deployment Script for LiveKit
# 
# Automatically detects platform (Mac/Linux) and deploys accordingly
#
# Usage: ./deploy.sh [OPTIONS]
#
# Options:
#   --platform [mac|linux]    Force specific platform
#   --env [dev|prod]          Deployment environment
#   --storage PATH            External storage path
#   --import PATH             Import configuration from backup
#   --help                    Show help
#
################################################################################

set -e

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

# Default values
PLATFORM=""
ENVIRONMENT="dev"
STORAGE_PATH=""
IMPORT_PATH=""
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

log_info() { echo -e "${BLUE}ℹ${NC} $1"; }
log_success() { echo -e "${GREEN}✓${NC} $1"; }
log_warning() { echo -e "${YELLOW}⚠${NC} $1"; }
log_error() { echo -e "${RED}✗${NC} $1"; }

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --platform)
            PLATFORM="$2"
            shift 2
            ;;
        --env)
            ENVIRONMENT="$2"
            shift 2
            ;;
        --storage)
            STORAGE_PATH="$2"
            shift 2
            ;;
        --import)
            IMPORT_PATH="$2"
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

# Detect platform if not specified
if [[ -z "$PLATFORM" ]]; then
    if [[ "$OSTYPE" == "darwin"* ]]; then
        PLATFORM="mac"
    elif [[ "$OSTYPE" == "linux-gnu"* ]]; then
        PLATFORM="linux"
    else
        log_error "Unsupported platform: $OSTYPE"
        exit 1
    fi
fi

log_info "Detected platform: $PLATFORM"
log_info "Environment: $ENVIRONMENT"

# Delegate to platform-specific script
case $PLATFORM in
    mac)
        INSTALL_SCRIPT="$SCRIPT_DIR/install-mac.sh"
        if [[ ! -f "$INSTALL_SCRIPT" ]]; then
            log_error "Mac installation script not found: $INSTALL_SCRIPT"
            exit 1
        fi
        
        ARGS=()
        [[ -n "$STORAGE_PATH" ]] && ARGS+=(--storage "$STORAGE_PATH")
        
        log_info "Launching Mac installation..."
        bash "$INSTALL_SCRIPT" "${ARGS[@]}"
        ;;
        
    linux)
        # For Linux (Hetzner), check if we're on a cloud instance
        if command -v hcloud &> /dev/null; then
            log_info "Hetzner Cloud environment detected"
        fi
        
        # Set default storage for Linux
        STORAGE_PATH="${STORAGE_PATH:-/opt/livekit}"
        
        log_info "Installing for Linux at: $STORAGE_PATH"
        
        # Run Linux installation steps
        "$SCRIPT_DIR/install-linux.sh" --storage "$STORAGE_PATH" --env "$ENVIRONMENT"
        ;;
        
    *)
        log_error "Unknown platform: $PLATFORM"
        exit 1
        ;;
esac

log_success "Deployment complete!"
