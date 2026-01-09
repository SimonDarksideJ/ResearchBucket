# LiveKit Deployment on Hetzner (Recommended: Docker Compose + Caddy)

## Overview

This guide covers the **easiest** way to run LiveKit on a Hetzner VPS/dedicated server:

- **LiveKit Server** in Docker (Docker Compose)
- **Caddy** as a reverse proxy providing automatic **TLS certificates** (Let’s Encrypt) for **HTTPS/WSS**

This approach is generally simpler than a native/systemd install because:

- upgrades/rollbacks are a `docker compose pull && docker compose up -d`
- configuration and logs are standardized
- TLS is handled automatically

## Table of Contents

1. [What You’ll Build](#what-youll-build)
2. [Prerequisites](#prerequisites)
3. [Step 1: Provision the Hetzner Server](#step-1-provision-the-hetzner-server)
4. [Step 2: Get the Public IP Address](#step-2-get-the-public-ip-address)
5. [Step 3: Point DNS to the Server](#step-3-point-dns-to-the-server)
6. [Step 4: Install Docker](#step-4-install-docker)
7. [Step 5: Open Firewall Ports](#step-5-open-firewall-ports)
8. [Step 6: Create LiveKit Config + API Keys](#step-6-create-livekit-config--api-keys)
9. [Step 7: Create Docker Compose Stack (LiveKit + Caddy)](#step-7-create-docker-compose-stack-livekit--caddy)
10. [Step 8: Configure SSL/WSS (Automatic)](#step-8-configure-sslwss-automatic)
11. [Step 9: Validate Connectivity](#step-9-validate-connectivity)
12. [Logs and Troubleshooting](#logs-and-troubleshooting)
13. [Appendix A: Native systemd (No Docker)](#appendix-a-native-systemd-no-docker)

---

## What You’ll Build

- Public endpoint for clients:
  - `https://media.myservice.net` (HTTPS)
  - `wss://media.myservice.net` (secure WebSocket signaling)
- WebRTC media ports reachable directly on the server:
  - UDP `50000-60000` (configurable)

---

## Prerequisites

- Hetzner **Cloud VPS** or **Dedicated** server
- Ubuntu 22.04 LTS (recommended; Ubuntu 20.04+ works)
- A domain name you control (for TLS): e.g. `media.myservice.net`
- Ability to create DNS records (A/AAAA)

Ports you will allow:

- TCP `80` (Let’s Encrypt HTTP-01 challenge)
- TCP `443` (HTTPS/WSS)
- UDP `50000-60000` (WebRTC RTP/RTCP media)

Optional (only if you enable TURN on the same host):

- UDP `3478` (TURN)

---

## Step 1: Provision the Hetzner Server

1. Create a server in Hetzner Cloud.
2. Choose:
   - Image: **Ubuntu 22.04**
   - Size: start with **4 vCPU / 8GB RAM** for production-ish workloads
3. Add your SSH key in the Hetzner UI.
4. Boot the server.

On first login:

```bash
sudo apt-get update
sudo apt-get -y upgrade
sudo reboot
```

---

## Step 2: Get the Public IP Address

You can find the public IPv4/IPv6 in the Hetzner Cloud Console, or from the server itself:

```bash
curl -4 https://ipv4.icanhazip.com
curl -6 https://ipv6.icanhazip.com
```

Keep this value; you’ll use it for DNS and (optionally) LiveKit node IP.

---

## Step 3: Point DNS to the Server

This is how you attach a **custom URL** (hostname) to your LiveKit deployment.

At a high level:

- Your DNS points `media.myservice.net` to the **public IP** of the Hetzner server.
- Caddy listens on `80/443` on that server and forwards traffic to the LiveKit container.

### DNS requirements

Create these DNS records at your DNS provider:

- **A record**: `media.myservice.net` → `<YOUR_SERVER_IPV4>`
- (Optional) **AAAA record**: `media.myservice.net` → `<YOUR_SERVER_IPV6>`

Notes:

- If you use Cloudflare, set the record to **DNS only** (not proxied) while issuing certificates and troubleshooting connectivity. (WebRTC media still requires direct UDP reachability.)
- Ensure TCP `80` and `443` are reachable from the internet; Caddy uses port `80` for the Let’s Encrypt HTTP-01 challenge.

Wait for propagation:

```bash
# Replace with your hostname
HOST=media.myservice.net

# See what IP DNS resolves to
getent ahosts "$HOST"
```

---

## Step 4: Install Docker

Install Docker Engine + Compose plugin:

```bash
# Install prerequisites
sudo apt-get update
sudo apt-get install -y ca-certificates curl gnupg

# Docker GPG key
sudo install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg
sudo chmod a+r /etc/apt/keyrings/docker.gpg

# Docker repository
echo \
  "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu \
  $(. /etc/os-release && echo $VERSION_CODENAME) stable" | \
  sudo tee /etc/apt/sources.list.d/docker.list > /dev/null

sudo apt-get update
sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin

# Optional: allow running docker without sudo
sudo usermod -aG docker $USER
newgrp docker
```

Verify:

```bash
docker version
docker compose version
```

---

## Step 5: Open Firewall Ports

### Option A: Hetzner Cloud Firewall (recommended)

In Hetzner Cloud → Firewalls, attach a firewall to the server:

- Allow inbound TCP `80`, `443`
- Allow inbound UDP `50000-60000`
- (Optional) allow inbound UDP `3478` if using TURN

### Option B: UFW on the server

```bash
sudo apt-get install -y ufw
sudo ufw allow OpenSSH
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
sudo ufw allow 50000:60000/udp
# Optional TURN:
# sudo ufw allow 3478/udp
sudo ufw --force enable
sudo ufw status
```

---

## Step 6: Create LiveKit Config + API Keys

Create a deployment directory:

```bash
mkdir -p ~/livekit-hetzner/{config,caddy,data}
cd ~/livekit-hetzner
```

### Generate API Key/Secret

Generate a strong API key and secret:

```bash
# Example: generate two random strings
API_KEY=$(openssl rand -hex 16)
API_SECRET=$(openssl rand -base64 32)

echo "API_KEY=$API_KEY"
echo "API_SECRET=$API_SECRET"
```

### Create the LiveKit config

Create `config/config.yaml`:

```yaml
port: 7880
bind_addresses:
  - "0.0.0.0"

rtc:
  port_range_start: 50000
  port_range_end: 60000
  use_ice_lite: true

  # Optional (if you want TCP-based RTC fallback):
  # tcp_port: 7881
  # udp_port: 7882

# Region label is informational (choose anything meaningful)
region: hetzner

# For many simple deployments you can omit node_ip.
# If you set it, use your server public IP.
# node_ip: "<YOUR_PUBLIC_IP>"

# Keys are injected via the LIVEKIT_KEYS environment variable in docker-compose.
# If you prefer, you can also hardcode keys here instead.

logging:
  level: info
  json: true

room:
  auto_create: true
  empty_timeout: 300
  max_participants: 50
```

Note: this guide injects auth keys via the `LIVEKIT_KEYS` environment variable in `docker-compose.yml`. If you prefer, you can instead hardcode a `keys:` map in `config/config.yaml`.

---

## Step 7: Create Docker Compose Stack (LiveKit + Caddy)

Create `.env` (keep it private):

```bash
cat > .env << 'EOF'
# Domain name used for HTTPS/WSS
LIVEKIT_DOMAIN=media.myservice.net

# LiveKit API key/secret (used for server auth token signing)
LIVEKIT_API_KEY=replace_me
LIVEKIT_API_SECRET=replace_me
EOF

chmod 600 .env
```

Edit `.env` and set:

- `LIVEKIT_DOMAIN`
- `LIVEKIT_API_KEY`
- `LIVEKIT_API_SECRET`

Create `docker-compose.yml`:

```yaml
version: "3.9"

services:
  livekit:
    image: livekit/livekit-server:latest
    container_name: livekit
    restart: unless-stopped

    # Publish only media ports publicly.
    # Keep HTTP API on localhost and expose via Caddy.
    ports:
      - "127.0.0.1:7880:7880/tcp"
      - "50000-60000:50000-60000/udp"

    volumes:
      - ./config:/config:ro
      - ./data:/data

    environment:
      - LIVEKIT_KEYS=${LIVEKIT_API_KEY}:${LIVEKIT_API_SECRET}

    command: ["--config", "/config/config.yaml"]

  caddy:
    image: caddy:2
    container_name: livekit-caddy
    restart: unless-stopped

    ports:
      - "80:80"
      - "443:443"

    volumes:
      - ./caddy/Caddyfile:/etc/caddy/Caddyfile:ro
      - caddy_data:/data
      - caddy_config:/config

    depends_on:
      - livekit

volumes:
  caddy_data:
  caddy_config:
```

---

## Step 8: Configure SSL/WSS (Automatic)

Caddy will automatically obtain and renew certificates for your domain.

Create `caddy/Caddyfile`:

```caddyfile
{$LIVEKIT_DOMAIN} {
  encode gzip

  # Reverse proxy to LiveKit HTTP port (includes WebSocket support)
  reverse_proxy livekit:7880
}
```

Start the stack:

```bash
docker compose up -d
```

If you change the hostname later, update `.env` (and/or the Caddyfile) and restart the proxy:

```bash
docker compose restart caddy
```

If certificates fail to issue:

- confirm DNS `A/AAAA` records are correct
- confirm ports `80` and `443` are reachable from the internet
- check Caddy logs (see Logs section)

---

## Step 9: Validate Connectivity

### 1) Check containers are healthy

```bash
docker compose ps
```

### 2) Confirm HTTP/HTTPS access

```bash
# Should return 200 (or JSON) once LiveKit is up
curl -I http://127.0.0.1:7880/health

# Should succeed over public HTTPS
curl -I https://$LIVEKIT_DOMAIN/health
```

### 3) Confirm WSS endpoint

WebSocket is served at the same host as HTTPS. Most clients will use:

- `wss://media.myservice.net`

If you want a quick smoke test:

```bash
# Install websocat (optional)
sudo apt-get install -y websocat

# Connect (this just checks that TLS + websocket handshake works)
websocat -v wss://$LIVEKIT_DOMAIN/
```

### 4) Confirm UDP media port reachability

The most common “it connects but no media” issue is UDP blocked. Ensure:

- Hetzner firewall allows UDP `50000-60000`
- UFW (if enabled) also allows it

---

## Logs and Troubleshooting

### LiveKit logs

```bash
# Follow logs
docker compose logs -f livekit

# Last 200 lines
docker compose logs --tail 200 livekit
```

### Caddy (TLS/proxy) logs

```bash
docker compose logs -f caddy
```

### Quick checks

```bash
# List listening ports
sudo ss -tulpn | grep -E ":(80|443)\b" || true
sudo ss -ulnp | grep -E ":5(0{4}|[0-9]{4})\b" || true

# Verify which IP the host resolves to
getent ahosts "$LIVEKIT_DOMAIN"
```

### Common issues

- **TLS certificate won’t issue**: DNS not pointing to the server, or ports 80/443 blocked.
- **WSS works but calls have no media**: UDP port range blocked (Hetzner firewall/UFW).
- **Clients connect only on LAN**: `node_ip` misconfigured; remove it, or set it to the server public IP.

---

## Notes on “Best/Easiest” Choice for Hetzner

On Hetzner, both native/systemd and Docker work well. For most teams, **Docker Compose + Caddy** wins on ease because it:

- reduces system-level config work
- standardizes logs/updates
- makes TLS setup almost “set and forget”

---

## Appendix A: Native systemd (No Docker)

This variant is the **lowest overhead** approach: LiveKit runs directly on the host under systemd.

To keep HTTPS/WSS simple, this appendix uses **Caddy as a host service** (not Docker) to terminate TLS and reverse proxy to LiveKit’s local HTTP port.

### Summary of Ports

- TCP `80` (Let’s Encrypt HTTP-01)
- TCP `443` (HTTPS/WSS)
- UDP `50000-60000` (WebRTC media)

### Step A1: Install LiveKit Server (binary)

Create a dedicated user and directories:

```bash
sudo useradd -r -s /bin/false -d /opt/livekit -m livekit
sudo mkdir -p /etc/livekit /var/log/livekit /var/lib/livekit
sudo chown -R livekit:livekit /etc/livekit /var/log/livekit /var/lib/livekit
```

Download and install the LiveKit server binary:

```bash
LIVEKIT_VERSION="v1.5.3"  # Update as needed

cd /tmp
wget "https://github.com/livekit/livekit/releases/download/${LIVEKIT_VERSION}/livekit-server_${LIVEKIT_VERSION}_linux_amd64.tar.gz"
tar -xzf "livekit-server_${LIVEKIT_VERSION}_linux_amd64.tar.gz"

sudo mv livekit-server /opt/livekit/livekit-server
sudo chown livekit:livekit /opt/livekit/livekit-server
sudo chmod 755 /opt/livekit/livekit-server
```

### Step A2: Configure API Key/Secret

Generate credentials:

```bash
API_KEY=$(openssl rand -hex 16)
API_SECRET=$(openssl rand -base64 32)
echo "API_KEY=$API_KEY"
echo "API_SECRET=$API_SECRET"
```

Create an environment file readable only by root:

```bash
sudo bash -c 'cat > /etc/livekit/livekit.env' << 'EOF'
LIVEKIT_KEYS=APIKey1:ReplaceWithSecret
EOF

sudo chmod 600 /etc/livekit/livekit.env
```

Edit `/etc/livekit/livekit.env` and set:

- `LIVEKIT_KEYS=<your_api_key>:<your_api_secret>`

### Step A3: Create `/etc/livekit/config.yaml`

```bash
sudo bash -c 'cat > /etc/livekit/config.yaml' << 'EOF'
port: 7880
bind_addresses:
  - "127.0.0.1"

rtc:
  port_range_start: 50000
  port_range_end: 60000
  use_ice_lite: true

region: hetzner

logging:
  level: info
  json: true

room:
  auto_create: true
  empty_timeout: 300
  max_participants: 50
EOF'

sudo chown livekit:livekit /etc/livekit/config.yaml
sudo chmod 640 /etc/livekit/config.yaml
```

Notes:

- Binding LiveKit to `127.0.0.1` keeps the HTTP API private; Caddy publishes HTTPS/WSS on `443`.
- If you want to expose LiveKit HTTP directly (not recommended), bind to `0.0.0.0` and open TCP `7880`.

### Step A4: Create systemd unit

Create `/etc/systemd/system/livekit.service`:

```ini
[Unit]
Description=LiveKit Media Server
Documentation=https://docs.livekit.io
After=network.target

[Service]
Type=simple
User=livekit
Group=livekit

EnvironmentFile=/etc/livekit/livekit.env
ExecStart=/opt/livekit/livekit-server --config /etc/livekit/config.yaml

Restart=on-failure
RestartSec=5s

NoNewPrivileges=true
PrivateTmp=true
ProtectSystem=strict
ProtectHome=true
ReadWritePaths=/var/log/livekit /var/lib/livekit

LimitNOFILE=65536

StandardOutput=journal
StandardError=journal
SyslogIdentifier=livekit

[Install]
WantedBy=multi-user.target
```

Enable and start:

```bash
sudo systemctl daemon-reload
sudo systemctl enable --now livekit
sudo systemctl status livekit --no-pager
```

### Step A5: Install and configure Caddy (TLS for HTTPS/WSS)

This publishes `https://<your-domain>` and `wss://<your-domain>` and proxies to LiveKit on localhost.

Install Caddy:

```bash
sudo apt-get update
sudo apt-get install -y debian-keyring debian-archive-keyring apt-transport-https curl

curl -1sLf 'https://dl.cloudsmith.io/public/caddy/stable/gpg.key' | sudo gpg --dearmor -o /usr/share/keyrings/caddy-stable-archive-keyring.gpg
curl -1sLf 'https://dl.cloudsmith.io/public/caddy/stable/debian.deb.txt' | sudo tee /etc/apt/sources.list.d/caddy-stable.list

sudo apt-get update
sudo apt-get install -y caddy
```

Create `/etc/caddy/Caddyfile`:

```caddyfile
media.myservice.net {
  encode gzip
  reverse_proxy 127.0.0.1:7880
}
```

Reload Caddy:

```bash
sudo systemctl reload caddy
sudo systemctl status caddy --no-pager
```

TLS notes:

- DNS A/AAAA must point to this server.
- TCP `80` and `443` must be reachable.

### Step A6: Firewall (same as Docker option)

Allow:

- TCP `80`, `443`
- UDP `50000-60000`

If using UFW:

```bash
sudo ufw allow OpenSSH
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
sudo ufw allow 50000:60000/udp
sudo ufw --force enable
sudo ufw status
```

### Step A7: Validate connectivity

```bash
curl -I http://127.0.0.1:7880/health
curl -I https://media.myservice.net/health
```

Client connection details:

- HTTPS base: `https://media.myservice.net`
- WebSocket signaling: `wss://media.myservice.net`

### Step A8: Logs (for testing)

LiveKit logs:

```bash
sudo journalctl -u livekit -f
sudo journalctl -u livekit -n 200 --no-pager
```

Caddy logs:

```bash
sudo journalctl -u caddy -f
sudo journalctl -u caddy -n 200 --no-pager
```

### When to choose systemd over Docker

- Choose **systemd** if you want the fewest moving parts and the smallest overhead.
- Choose **Docker Compose** if you want the easiest upgrades/rollbacks and a more self-contained deployment.
