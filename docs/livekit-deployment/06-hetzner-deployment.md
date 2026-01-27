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

## What Hetzner Provides vs. What You Configure

Understanding what Hetzner handles and what you need to set up yourself:

### ✅ What Hetzner Provides

| Component | Details |
|-----------|---------|
| **Server/VM** | Virtual or dedicated server hardware |
| **Public IP** | Static IPv4 and/or IPv6 address |
| **Network Bandwidth** | 20 TB/month included (Cloud), unlimited (Dedicated) |
| **Firewall** | Basic firewall rules (you configure) |
| **Server Access** | SSH access, root credentials |
| **Data Center** | Physical infrastructure, power, cooling |
| **Backups** | Optional paid backups |

### 🔧 What You Need to Configure

| Component | Your Responsibility | This Guide Covers |
|-----------|---------------------|-------------------|
| **DNS Records** | Point your domain to server IP | ✅ Yes (Step 3) |
| **Operating System** | Install/update Ubuntu | ✅ Yes (Step 1) |
| **Docker** | Install Docker Engine | ✅ Yes (Step 4) |
| **Firewall Rules** | Open required ports | ✅ Yes (Step 5) |
| **LiveKit Config** | Create configuration files | ✅ Yes (Step 6) |
| **SSL Certificates** | Obtain/renew certificates | ✅ Automatic (Caddy) |
| **Reverse Proxy** | Set up Caddy | ✅ Yes (Step 7-8) |
| **Domain Name** | Own/purchase domain | ⚠️ External |
| **DNS Hosting** | Host DNS records | ⚠️ External (any provider) |

### ❌ What Hetzner Does NOT Provide

- Domain name registration (buy separately)
- DNS hosting (use any DNS provider)
- SSL certificates (but Caddy gets them automatically)
- Pre-configured LiveKit (you install it)
- Application support (community/self-support)

---

## External Configuration Requirements

This section explains all the external services and configurations you'll need.

### 1. Domain Name

**What is it?**
- A domain name like `media.myservice.net` that points to your server
- Required for SSL/TLS certificates (HTTPS/WSS)
- Makes your server accessible via a memorable name

**Where to get one:**
- [Namecheap](https://namecheap.com) - $9-12/year (.com)
- [Google Domains](https://domains.google) - $12/year (.com)  
- [Cloudflare Registrar](https://www.cloudflare.com/products/registrar/) - $8-9/year (at-cost)
- [Porkbun](https://porkbun.com) - $8-10/year (.com)
- **Use existing domain** - Can use a subdomain of one you already own

**Cost:**
- .com domains: $8-15/year
- .net domains: $10-18/year
- .io domains: $30-40/year (avoid if budget-conscious)

**💡 Recommendation**: Buy a `.com` or `.net` domain from Namecheap or Porkbun ($8-12/year)

### 2. DNS Management (You Manage Your Own)

**What is it?**
- DNS (Domain Name System) translates your domain to your server's IP address
- You need to create an "A record" pointing to your Hetzner server
- Completely separate from Hetzner - use any DNS provider

**DNS Provider Options:**

#### Option A: Cloudflare DNS (Recommended, Free)
**Cost**: ✅ Free forever  
**Pros**: Fast, reliable, DDoS protection, easy interface  
**Cons**: Need to change nameservers  
**Setup Link**: [dash.cloudflare.com](https://dash.cloudflare.com)

**Setup:**
1. Sign up at [dash.cloudflare.com](https://dash.cloudflare.com)
2. Add your domain
3. Change nameservers at your domain registrar
4. Create A record: `media.myservice.net` → `your_server_ip`

#### Option B: Domain Registrar's DNS
**Cost**: ✅ Usually included with domain  
**Pros**: Already set up, no nameserver changes  
**Cons**: Often slower, fewer features

**Setup:**
1. Log into your domain registrar (Namecheap, Google, etc.)
2. Find DNS settings or "DNS Management"
3. Create A record: `media.myservice.net` → `your_server_ip`

#### Option C: Hetzner DNS
**Cost**: ✅ Free  
**Pros**: Integrated with Hetzner, simple  
**Cons**: Less features than Cloudflare  
**Setup Link**: [dns.hetzner.com](https://dns.hetzner.com)

**Setup:**
1. Go to [dns.hetzner.com](https://dns.hetzner.com)
2. Add your domain
3. Update nameservers at registrar:
   - `hydrogen.ns.hetzner.com`
   - `oxygen.ns.hetzner.com`
   - `helium.ns.hetzner.de`
4. Create A record in Hetzner DNS console

#### Option D: Route 53 (AWS)
**Cost**: ~$0.50/month + queries  
**Pros**: Highly reliable, programmatic access  
**Cons**: Not free, more complex

**💡 Recommendation**: Use **Cloudflare DNS** (free, fast, easy) or your **registrar's DNS** (already set up)

### 3. SSL/TLS Certificates (Automatic!)

**What is it?**
- HTTPS requires an SSL certificate to encrypt traffic
- LiveKit requires WSS (WebSocket Secure) which also needs SSL
- Without this, modern browsers won't allow media access

**How it works in this guide:**
- ✅ **Fully automatic** - Caddy handles everything
- Caddy uses Let's Encrypt (free certificate authority)
- Certificates auto-renew every 90 days
- No manual work required after initial setup

**Requirements for automatic SSL:**
1. Valid domain name pointing to your server
2. Port 80 accessible (for Let's Encrypt verification)
3. Port 443 accessible (for HTTPS traffic)
4. DNS propagated (A record resolves correctly)

**Cost**: ✅ Free (Let's Encrypt is free forever)

---

## Complete External Access Setup

Here's everything you need for external access in one place:

### Checklist

- [ ] **Domain purchased** (or using existing subdomain)
- [ ] **DNS A record created** (pointing to server IP)
- [ ] **DNS propagated** (verified with `nslookup`)
- [ ] **Hetzner server provisioned** (Ubuntu 22.04)
- [ ] **Firewall ports open** (80, 443, 50000-60000)
- [ ] **Caddy configured** (with your domain in Caddyfile)
- [ ] **SSL certificate obtained** (automatic via Caddy)
- [ ] **LiveKit accessible** (test with `curl https://yourdomain/health`)

### Full Setup Summary

1. **Buy domain** ($10-15/year) or use existing
2. **Point DNS** to Hetzner server IP (free, 5 minutes)
3. **Configure Hetzner firewall** (free, built-in)
4. **Install Docker** (free, this guide)
5. **Deploy LiveKit + Caddy** (free, this guide)
6. **Caddy gets SSL cert** (automatic, free)
7. **Access via** `https://media.yourdomain.com`

**Total external costs**: $10-15/year (domain only)

### What You DON'T Need

❌ Load balancer (unless scaling beyond 1 server)  
❌ CDN (LiveKit serves direct)  
❌ VPN (ports are publicly accessible)  
❌ SSL certificate service (Caddy + Let's Encrypt is free)  
❌ DDoS protection (basic included, Cloudflare DNS optional)  
❌ Monitoring service (optional, can add Grafana)

---

## Cost Summary

### One-Time Costs
- Domain name: $8-15 (annual, but paid once per year)

### Monthly Costs
- Hetzner Cloud VPS: From €4.15/month (~$4.50)
- DNS hosting: Free (use Cloudflare, Hetzner, or registrar)
- SSL certificates: Free (Let's Encrypt)
- Bandwidth: Included (20 TB/month on Cloud)

### Total First Year
- Domain: $10
- Hetzner (12 months): €50-60 (~$55-65)
- **Total**: ~$65-75/year

### Compared to Alternatives
- AWS/GCP equivalent: $100-200/year
- Managed LiveKit: $200-500/year
- Other VPS: $60-120/year

**Hetzner is one of the most cost-effective options for self-hosting LiveKit.**

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

## Step 3: Point DNS to the Server (Detailed)

This is the most important external configuration step. Follow carefully.

### Overview

Your DNS points `media.myservice.net` to the **public IP** of the Hetzner server. Once configured:
- Caddy listens on ports 80/443
- Forwards traffic to LiveKit container
- Automatically obtains SSL certificate from Let's Encrypt

### DNS Requirements

Create these DNS records at your DNS provider:

- **A record**: `media.myservice.net` → `<YOUR_SERVER_IPV4>`
- (Optional) **AAAA record**: `media.myservice.net` → `<YOUR_SERVER_IPV6>`

### Detailed DNS Setup by Provider

#### Cloudflare (Recommended)

1. **Sign up / Log in**
   - Go to [dash.cloudflare.com](https://dash.cloudflare.com)
   - Create account or log in

2. **Add Your Domain**
   - Click "Add a Site"
   - Enter your domain (e.g., `myservice.net`)
   - Choose "Free" plan
   - Click "Continue"

3. **Change Nameservers**
   - Cloudflare shows you 2 nameservers
   - Go to your domain registrar (where you bought domain)
   - Update nameservers to Cloudflare's
   - Wait 5-60 minutes for propagation
   - Return to Cloudflare, click "Done, check nameservers"

4. **Create A Record**
   - Click on DNS → Records
   - Click "Add record"
   - Type: **A**
   - Name: **media** (or whatever subdomain you want)
   - IPv4 address: **your_server_ip** (from Step 2)
   - Proxy status: **🟠 DNS only** (IMPORTANT - turn off proxy!)
   - TTL: **Auto**
   - Click "Save"

**Why DNS only?**: WebRTC needs direct UDP access. Cloudflare proxy only supports TCP/HTTP/HTTPS, not UDP.

#### Namecheap

1. **Log into Namecheap**
   - Go to [namecheap.com](https://namecheap.com)
   - Log into your account

2. **Manage Domain**
   - Domain List → Find your domain
   - Click "Manage"

3. **Advanced DNS**
   - Click "Advanced DNS" tab

4. **Add A Record**
   - Click "Add New Record"
   - Type: **A Record**
   - Host: **media** (for media.yourdomain.com)
   - Value: **your_server_ip**
   - TTL: **Automatic**
   - Click the checkmark to save

5. **Wait for Propagation**
   - Usually takes 5-30 minutes
   - Can take up to 24 hours

#### Hetzner DNS

1. **Go to Hetzner DNS**
   - Visit [dns.hetzner.com](https://dns.hetzner.com)
   - Log in with Hetzner account

2. **Add Zone**
   - Click "Add Zone"
   - Enter your domain
   - Click "Continue"

3. **Update Nameservers**
   - Hetzner DNS shows nameservers:
     - `hydrogen.ns.hetzner.com`
     - `oxygen.ns.hetzner.com`
     - `helium.ns.hetzner.de`
   - Go to your domain registrar
   - Update nameservers to these
   - Wait for propagation

4. **Add A Record**
   - Click on your zone
   - Click "Add Record"
   - Name: **media**
   - Type: **A**
   - Value: **your_server_ip**
   - TTL: **86400** (24 hours)
   - Click "Create Record"

#### Using Registrar's DNS (Generic)

If your domain is at Namecheap, Google Domains, GoDaddy, etc., you can use their DNS:

1. **Log into registrar**
2. **Find DNS settings**
   - Look for "DNS Management", "DNS Settings", or "Nameservers"
3. **Add A Record**
   - Host/Name: `media` (or your subdomain)
   - Type: `A`
   - Points to / Value: your server IP
   - TTL: Auto or 3600
4. **Save changes**

### Verify DNS Configuration

Wait 5-10 minutes after creating records, then test:

```bash
# Test with nslookup
nslookup media.myservice.net

# Should show:
# Server: ...
# Address: ...
# 
# Non-authoritative answer:
# Name: media.myservice.net
# Address: YOUR_SERVER_IP

# Test with dig (more detailed)
dig media.myservice.net

# Test with ping
ping media.myservice.net

# Should ping your server IP
```

### Troubleshooting DNS

**Problem: nslookup returns NXDOMAIN**
- DNS record doesn't exist yet
- Wait longer (up to 24 hours)
- Check you created the record correctly
- Verify domain nameservers are set correctly

**Problem: Returns wrong IP**
- Old DNS record cached
- Try from different computer/network
- Use DNS checker: [whatsmydns.net](https://whatsmydns.net)
- Wait for TTL to expire

**Problem: Works on one device but not another**
- DNS caching issue
- Flush DNS cache:
  ```bash
  # Mac
  sudo dscacheutil -flushcache; sudo killall -HUP mDNSResponder
  
  # Linux
  sudo systemd-resolve --flush-caches
  
  # Windows
  ipconfig /flushdns
  ```

### Important Notes

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
