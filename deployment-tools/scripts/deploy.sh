#!/usr/bin/env bash

################################################################################
# Universal Deployment Script for LiveKit
#
# Selects the platform-specific installer and forwards the relevant arguments.
#
# Usage: ./deploy.sh [OPTIONS]
#
# Options:
#   --platform [mac|linux]    Force a specific platform
#   --env [dev|production]    Deployment environment (linux only)
#   --storage PATH            Installation path
#   --domain DOMAIN           Public LiveKit hostname (linux production)
#   --email EMAIL             ACME email for Caddy (linux production)
#   --grafana-pass PASSWORD   Grafana admin password
#   --skip-deps               Skip package installation
#   --import PATH             Accepted for compatibility; not applied automatically
#   --help                    Show help
#
################################################################################

set -euo pipefail

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

PLATFORM=""
ENVIRONMENT="dev"
STORAGE_PATH=""
DOMAIN=""
EMAIL=""
IMPORT_PATH=""
SKIP_DEPS=false
GRAFANA_PASSWORD=""
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

log_info() { echo -e "${BLUE}ℹ${NC} $1"; }
log_success() { echo -e "${GREEN}✓${NC} $1"; }
log_warning() { echo -e "${YELLOW}⚠${NC} $1"; }
log_error() { echo -e "${RED}✗${NC} $1"; }

show_help() {
    grep '^#' "$0" | sed 's/^# //' | sed 's/^#//'
}

while [[ $# -gt 0 ]]; do
    case "$1" in
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
        --import)
            IMPORT_PATH="$2"
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

if [[ -z "$PLATFORM" ]]; then
    case "$OSTYPE" in
        darwin*)
            PLATFORM="mac"
            ;;
        linux-gnu*|linux*)
            PLATFORM="linux"
            ;;
        *)
            log_error "Unsupported platform: $OSTYPE"
            exit 1
            ;;
    esac
fi

log_info "Platform: $PLATFORM"
log_info "Environment: $ENVIRONMENT"

if [[ -n "$IMPORT_PATH" ]]; then
    log_warning "--import is currently documentation-only. Restore the backup contents before rerunning the installer."
fi

case "$PLATFORM" in
    mac)
        INSTALL_SCRIPT="$SCRIPT_DIR/install-mac.sh"
        if [[ ! -f "$INSTALL_SCRIPT" ]]; then
            log_error "Mac installation script not found: $INSTALL_SCRIPT"
            exit 1
        fi

        ARGS=()
        [[ -n "$STORAGE_PATH" ]] && ARGS+=(--storage "$STORAGE_PATH")
        [[ -n "$GRAFANA_PASSWORD" ]] && ARGS+=(--grafana-pass "$GRAFANA_PASSWORD")
        [[ "$SKIP_DEPS" == true ]] && ARGS+=(--skip-deps)

        log_info "Launching macOS installer"
        bash "$INSTALL_SCRIPT" "${ARGS[@]}"
        ;;

    linux)
        INSTALL_SCRIPT="$SCRIPT_DIR/install-linux.sh"
        if [[ ! -f "$INSTALL_SCRIPT" ]]; then
            log_error "Linux installation script not found: $INSTALL_SCRIPT"
            exit 1
        fi

        ARGS=(--env "$ENVIRONMENT")
        [[ -n "$STORAGE_PATH" ]] && ARGS+=(--storage "$STORAGE_PATH")
        [[ -n "$DOMAIN" ]] && ARGS+=(--domain "$DOMAIN")
        [[ -n "$EMAIL" ]] && ARGS+=(--email "$EMAIL")
        [[ -n "$GRAFANA_PASSWORD" ]] && ARGS+=(--grafana-pass "$GRAFANA_PASSWORD")
        [[ "$SKIP_DEPS" == true ]] && ARGS+=(--skip-deps)

        log_info "Launching Linux installer"
        bash "$INSTALL_SCRIPT" "${ARGS[@]}"
        ;;

    *)
        log_error "Unknown platform: $PLATFORM"
        exit 1
        ;;
esac

log_success "Deployment workflow finished"
