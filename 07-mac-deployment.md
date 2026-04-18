# Mac LiveKit Test Deployment Guide

## Intent

This guide is for a local Mac-based LiveKit environment that mirrors the Linux stack closely enough for development, integration work, and controlled external tests.

It is not a replacement for a public Linux production node.

The main correction in this refresh is networking:

- a reverse proxy helps with HTTPS and signaling,
- but LiveKit still needs routed UDP and TCP media paths,
- so a Cloudflare Tunnel alone is not enough for realistic public browser testing.

## What The Mac Stack Includes

The current macOS installer provisions:

- LiveKit
- Redis
- Prometheus
- Grafana
- Loki
- Promtail

It intentionally avoids pretending that a local Mac and an HTTP tunnel form a production-ready public media edge.

## Supported Use Cases

Use the Mac stack for:

- local development,
- token generation and app integration,
- browser and SDK checks on the same LAN,
- external validation only after a real routed path exists.

Do not use it as the final answer for:

- always-on production,
- large public rooms,
- SLA-sensitive workloads,
- or capacity claims without a load test.

## Read This First If You Do Not Normally Use macOS

- Finder is the macOS file browser.
- Terminal is the shell application in `Applications > Utilities > Terminal`.
- `~/` means the current user's home folder.
- `/Volumes/DriveName` is where macOS mounts external drives. Only use a drive name that actually exists.
- Docker Desktop is a GUI application. The shell commands in this guide do not work until Docker Desktop has been installed, launched once, and allowed to finish its first-run setup.

## Before You Start

1. Sign in to a Mac account with administrator rights.
1. Install Docker Desktop for Mac from <https://www.docker.com/products/docker-desktop>.
1. Launch Docker Desktop once and approve any prompts for the privileged helper, networking, or file access.
1. Open Terminal and verify Docker is ready:

```bash
docker version
```

1. If Git is missing, install the Apple command line tools:

```bash
xcode-select --install
```

## Storage Path Choice

If you are unsure about macOS storage paths, do not pass `--storage` on the first run.

- safest first run: install on the internal disk at `~/livekit-data`
- installer shortcut: `~/livekit` points to the actual install directory
- external drive example: `"/Volumes/SamsungT7/livekit"`

To see which external drives are actually mounted:

```bash
ls /Volumes
```

If the drive name does not appear there, do not use it in the storage path yet.

## Step-By-Step Installation

### 1. Open Terminal

Use Spotlight, type `Terminal`, and open the Terminal app.

### 2. Clone the repository

```bash
git clone https://github.com/SimonDarksideJ/ResearchBucket.git
cd ResearchBucket/deployment-tools
```

### 3. Run the installer

Safest first run on the internal disk:

```bash
./scripts/install-mac.sh
```

If you specifically want to use an external drive that is already mounted:

```bash
./scripts/install-mac.sh --storage "/Volumes/SamsungT7/livekit"
```

If you want to set the Grafana password yourself instead of letting the installer generate one:

```bash
./scripts/install-mac.sh --grafana-pass "choose-a-strong-password"
```

### 4. Let the first run finish

The first run pulls several container images and can take a few minutes.

If Docker Desktop opens a permissions dialog during the install, approve it and rerun the installer if needed.

After installation, the local endpoints are:

- LiveKit: `http://localhost:7880`
- Grafana: `http://localhost:3000`
- Prometheus: `http://localhost:9090`

## Local Requirements

- macOS 11 or later
- Docker Desktop
- 8 GB RAM minimum, 16 GB preferred
- external storage optional, but recommended if you intend to keep logs, backups, or repeated test artifacts

The installer creates the stack under the chosen storage path and links it to `~/livekit`.

## What The Installer Creates

The installer writes the stack to the chosen storage path and creates a shortcut symlink at `~/livekit`.

Main items created:

- `~/livekit/config/livekit.yaml`
- `~/livekit/docker-compose.yml`
- `~/livekit/.env`
- `~/livekit/.credentials`
- `~/livekit/start.sh`
- `~/livekit/stop.sh`
- `~/livekit/status.sh`
- `~/livekit/logs.sh`

## First Verification

Run these checks after the installer completes.

### Check container state

```bash
cd ~/livekit
./status.sh
```

### Check the local endpoints

```bash
curl -I http://localhost:7880
curl -I http://localhost:3000/login
curl -I http://localhost:9090/-/healthy
```

### Check generated credentials

```bash
cat ~/livekit/.credentials
```

### If something does not start

```bash
cd ~/livekit
./logs.sh livekit
./logs.sh grafana
docker compose ps
```

## Operational Commands

Normal day-to-day commands:

```bash
cd ~/livekit
./start.sh
./stop.sh
./status.sh
./logs.sh
./logs.sh livekit
```

## Networking Modes

### 1. Local-Only Mode

This is the default and safest mode.

Use it when:

- you are testing the app locally,
- you only need LAN access,
- or you are not ready to route public media.

### 2. Direct Home Router Exposure

This only works if your ISP gives you a reachable public IP and you control the router.

You must preserve:

- HTTPS or another valid signaling path for the browser,
- `7881/tcp` for ICE or TCP fallback,
- `3478/udp` for TURN over UDP,
- `50000-50100/udp` for the test media range.

If you cannot forward those ports, stop here and use an edge relay.

### 3. Public Edge Relay Or VPS

This is the recommended public-test shape when the Mac sits behind ISP-controlled NAT or CGNAT.

The pattern is:

1. A small public Linux host or VPS gets the public IP.
2. A WireGuard tunnel connects that host to the Mac.
3. The relay terminates HTTPS for the LiveKit hostname.
4. The relay forwards the required TCP and UDP ports to the Mac over WireGuard.

That gives the Mac a stable public edge without asking the ISP to expose inbound ports directly.

## Edge Relay Reference Pattern

### WireGuard Addressing Example

- relay host: `10.20.0.1`
- Mac: `10.20.0.2`

### LiveKit Config Change On The Mac

Edit `~/livekit/config/livekit.yaml` so the Mac advertises the relay's public IP instead of discovering the home IP:

```yaml
rtc:
  tcp_port: 7881
  port_range_start: 50000
  port_range_end: 50100
  use_external_ip: false
  node_ip: RELAY_PUBLIC_IP
```

### Caddy On The Relay Host

```caddyfile
livekit-lab.example.com {
    reverse_proxy 10.20.0.2:7880
}
```

### Port Forwarding On The Relay Host

You still need to forward LiveKit's media ports to the Mac. Example Linux DNAT shape:

```bash
sysctl -w net.ipv4.ip_forward=1

iptables -t nat -A PREROUTING -p tcp --dport 7881 -j DNAT --to-destination 10.20.0.2:7881
iptables -t nat -A PREROUTING -p udp --dport 3478 -j DNAT --to-destination 10.20.0.2:3478
iptables -t nat -A PREROUTING -p udp --dport 50000:50100 -j DNAT --to-destination 10.20.0.2
iptables -t nat -A POSTROUTING -d 10.20.0.2 -j MASQUERADE
```

Use persistent firewall tooling on the relay host rather than leaving ad hoc iptables rules as the final state.

## Why Cloudflare Tunnel Is Still Useful

Cloudflare Tunnel still has value for:

- Grafana access,
- temporary admin access,
- limited signaling checks.

It is just not the whole answer for public media.

Examples:

```bash
./scripts/setup-cloudflare-tunnel.sh --service grafana
./scripts/setup-cloudflare-tunnel.sh --service livekit --hostname livekit-admin.example.com
```

Treat that as an admin convenience, not as proof that the media plane is correct.

## Ports Used By The Mac Test Stack

| Port | Purpose |
| ---- | ------- |
| `7880/tcp` | local signaling endpoint |
| `7881/tcp` | ICE or TCP fallback |
| `3478/udp` | TURN over UDP |
| `50000-50100/udp` | reduced test media range |
| `3000/tcp` | Grafana local UI |
| `9090/tcp` | Prometheus local UI |

## Remote Verification Checklist

Once you add router forwarding or an edge relay:

1. test from a network that is not the Mac's home LAN,
2. confirm the browser can join and exchange media, not just open the signaling WebSocket,
3. verify UDP is actually being used where expected,
4. watch `~/livekit/logs.sh livekit` and Grafana while the remote test runs,
5. run a small load test before relying on the setup.

## Common Failure Points For Non-Mac Users

- Docker Desktop is installed but not actually running.
- The chosen `--storage` path points to a drive name that is not mounted under `/Volumes`.
- The first Docker Desktop launch is still waiting for GUI approval.
- Grafana or the LiveKit endpoint opens locally, but remote media still fails because the UDP ports are not routed.
- Testing happened only on the same LAN, so the public path was never really validated.

## Practical Recommendation

- Keep the Mac environment for development and integration.
- Use an edge relay only when you genuinely need public remote browser tests.
- Move to the Hetzner Linux deployment before you call the environment production-ready.

## Related Documents

- [06-hetzner-deployment.md](06-hetzner-deployment.md)
- [Linux-Hosting-Research-LiveKit.md](Linux-Hosting-Research-LiveKit.md)
- [deployment-tools/README.md](deployment-tools/README.md)
