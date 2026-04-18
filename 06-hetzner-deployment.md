# Hetzner Deployment Guide

## Intent

This guide is the current production-oriented path for self-hosting LiveKit on a public Linux VM, with Hetzner Cloud as the reference platform.

It replaces the earlier assumption that a very small node plus generous bandwidth was enough to declare a deployment viable. The refreshed position is:

- start with a real public Linux host,
- expose the required TCP and UDP ports,
- terminate HTTPS cleanly,
- keep monitoring and logs from day one,
- and validate your room shape with load tests before making capacity promises.

## Validated LiveKit Assumptions

These are the constraints this guide is built around:

- each room must fit on one node,
- Redis-backed distributed mode improves room count and resilience, not single-room size,
- CPU and bandwidth are the main scaling bounds,
- public browser traffic needs routed media ports, not only an HTTPS reverse proxy,
- and official benchmark examples are on a 16-core compute-oriented host, not a small shared VM.

## Recommended Hetzner Shapes

| Use case | Suggested shape |
| -------- | --------------- |
| Staging or pilot | Shared 2 vCPU / 8 GB class VM |
| Small production | Dedicated 4+ vCPU / 16+ GB class VM |
| Larger or maintenance-sensitive production | Larger dedicated node or multi-node LiveKit with Redis |

Use shared nodes to validate deployment behavior. Use dedicated CPU once you care about predictable external user experience.

## What The Installer Provisions

The current Linux installer provisions:

- LiveKit server
- Redis
- Caddy for HTTPS termination to the signaling endpoint
- Prometheus
- Grafana
- Loki
- Promtail
- Node Exporter

This repository intentionally gives you a solid single-node baseline. It is not trying to hide multi-node complexity behind a misleading one-command promise.

## Prerequisites

Before you start, have the following ready:

1. A Hetzner Cloud account.
2. A domain you control.
3. DNS access for that domain.
4. An SSH key uploaded to Hetzner.
5. A fresh Ubuntu 24.04 LTS VM or a similar Debian-family Linux image.

## Public Ports

Open these ports in the Hetzner Cloud Firewall and any host firewall:

| Port | Purpose |
| ---- | ------- |
| `80/tcp` | ACME challenge and HTTP handling |
| `443/tcp` | HTTPS and WebSocket signaling |
| `7881/tcp` | ICE over TCP fallback |
| `3478/udp` | TURN over UDP |
| `50000-60000/udp` | WebRTC media range |

Do not hide the UDP ports behind an HTTP-only tunnel and still expect browser media to behave like production.

## DNS

Point your LiveKit hostname to the public VM IP.

Example record:

```text
Type: A
Name: livekit
Target: YOUR_PUBLIC_IP
Proxy/CDN: OFF
TTL: Auto
```

If you use Cloudflare DNS, keep the LiveKit record unproxied. Cloudflare Tunnel and HTTP proxying are useful for admin surfaces, but they do not replace the routed UDP media path.

The hostname should resolve correctly before you expect Caddy to obtain certificates.

## Server Creation Notes

For a fresh Hetzner VM:

- region: choose the region closest to your users, usually Germany or Finland for EU-first workloads,
- image: Ubuntu 24.04 LTS,
- type: shared for lab or pilot, dedicated for production,
- networking: public IPv4 plus IPv6 if you want it,
- attach your SSH key at creation time.

## Deployment Steps

### 1. Clone the repository on the server

```bash
git clone https://github.com/SimonDarksideJ/ResearchBucket.git
cd ResearchBucket/deployment-tools
```

### 2. Run the Linux installer

```bash
sudo ./scripts/deploy.sh \
  --platform linux \
  --env production \
  --domain livekit.example.com \
  --email ops@example.com
```

That installs packages, creates `/opt/livekit`, writes the current config files, pulls the images, and starts the stack.

### 3. Verify the public endpoint

```bash
curl -I https://livekit.example.com/healthz
```

Expected result: HTTP `200` after DNS and certificate issuance settle.

### 4. Access monitoring through SSH tunnels

Grafana and Prometheus are bound to localhost on the server by default.

```bash
ssh -L 3000:127.0.0.1:3000 root@YOUR_SERVER_IP
ssh -L 9090:127.0.0.1:9090 root@YOUR_SERVER_IP
```

Then open:

- `http://localhost:3000`
- `http://localhost:9090`

## Runtime Layout

The installer writes the deployment under `/opt/livekit`:

```text
/opt/livekit/
├── config/
├── data/
├── logs/
├── monitoring/
├── docker-compose.yml
├── .env
└── .credentials
```

Useful commands after installation:

```bash
cd /opt/livekit
./status.sh
./logs.sh livekit
./start.sh
./stop.sh
```

## Operational Notes

### Redis Is Included On Purpose

Current LiveKit guidance recommends Redis for production deployments and requires it for distributed mode. Even on a single node, including Redis now avoids a second architectural correction later.

### This Repository Does Not Fully Automate TURN/TLS On 443

The stack in this repo exposes:

- HTTPS signaling on `443/tcp`
- TCP fallback on `7881/tcp`
- TURN or UDP relay on `3478/udp`
- media on `50000-60000/udp`

That is a solid public baseline. If your users sit behind strict firewalls that only allow outbound TLS, treat the official LiveKit VM generator and current LiveKit networking docs as the source of truth for a TURN/TLS-on-443 deployment.

### Shared VMs Are Still Not Capacity Guarantees

Use the shared-node path to prove configuration, DNS, certificates, tokens, and basic room behavior. Move to dedicated CPU or larger nodes before you promise production SLOs.

## Load Testing Before Launch

Do not use this guide as a proxy for capacity planning. After deployment:

1. install the LiveKit CLI on a test machine,
2. run `lk load-test` against the public URL,
3. model your real room shape,
4. watch CPU, memory, and reconnect behavior in Grafana,
5. resize the VM before launch if headroom is thin.

## Backup And Maintenance

Create a backup artifact:

```bash
cd /opt/livekit
./backup.sh --compress
```

Run a health check:

```bash
cd /opt/livekit
./health-check.sh --verbose
```

## When To Move Beyond This Guide

Use a more advanced topology when you need one of these:

- multiple active nodes,
- draining nodes during maintenance,
- multi-region routing,
- strict TURN/TLS requirements,
- ingress or egress heavy workloads,
- or very large single rooms.

At that point, keep this repo for baseline patterns and move the node topology decisions closer to the official LiveKit distributed guidance.
