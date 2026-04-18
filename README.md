# ResearchBucket

Current research and deployment assets for self-hosting LiveKit, centered on a Hetzner Linux deployment and a Mac-based test environment.

## What Changed

The repository was refreshed against current LiveKit self-hosting documentation and current Hetzner positioning. The key corrections are:

- LiveKit room capacity is constrained per node. Multi-node Redis-backed deployments increase room count and redundancy, but a single room still has to fit on one node.
- Prior sizing based only on monthly bandwidth was misleading. CPU, publisher/subscriber mix, simulcast, and direct UDP reachability are the primary constraints.
- Public browser connectivity requires routed TCP and UDP media paths. Reverse proxies and Cloudflare Tunnel help with HTTPS and limited signaling exposure, but they do not replace LiveKit's required media ports.
- The repository now includes a real Linux installer, a corrected macOS test stack, and updated monitoring defaults.

## Quick Start

Quick reference: [QUICK-REFERENCE.md](QUICK-REFERENCE.md)

Hetzner or other Linux VM:

```bash
cd deployment-tools
sudo ./scripts/deploy.sh \
    --platform linux \
    --env production \
    --domain livekit.example.com \
    --email ops@example.com
```

Mac local test stack on the Mac's internal disk:

```bash
cd deployment-tools
./scripts/install-mac.sh
```

If you want to use an external drive and know its mounted name, pass `--storage "/Volumes/DriveName/livekit"`.

## Recommended Reading Order

1. [Linux-Hosting-Research-LiveKit.md](Linux-Hosting-Research-LiveKit.md)
2. [06-hetzner-deployment.md](06-hetzner-deployment.md)
3. [07-mac-deployment.md](07-mac-deployment.md)
4. [deployment-tools/README.md](deployment-tools/README.md)

## Current Position

- Hetzner remains a strong fit for EU-first self-hosted LiveKit because bandwidth is cheap, 20 TB is included, and the pricing remains materially lower than hyperscalers.
- Shared vCPU plans are appropriate for lab, staging, and small pilots. Predictable production starts on dedicated CPU plans or larger single nodes.
- The Mac path is for development, integration, and controlled external testing. If the Mac sits behind ISP-controlled NAT or CGNAT, use a public edge relay or VPS to terminate HTTPS and forward the required TCP and UDP ports.
- Load-test your own workload before promising capacity. LiveKit's published benchmarks use a 16-core compute-optimized host, which is not directly comparable to a 2 vCPU cloud VM or a laptop.

## Repository Layout

```text
ResearchBucket/
├── README.md
├── QUICK-REFERENCE.md
├── Linux-Hosting-Research-LiveKit.md
├── 06-hetzner-deployment.md
├── 07-mac-deployment.md
└── deployment-tools/
        ├── README.md
        ├── scripts/
        │   ├── deploy.sh
        │   ├── install-linux.sh
        │   ├── install-mac.sh
        │   ├── health-check.sh
        │   ├── backup.sh
        │   └── setup-cloudflare-tunnel.sh
        └── monitoring/
                ├── prometheus/
                ├── grafana/
                └── loki/
```

## Tooling Notes

- Linux installs provision LiveKit, Redis, Caddy, Prometheus, Grafana, Loki, Promtail, and Node Exporter.
- macOS installs provision LiveKit, Redis, Prometheus, Grafana, Loki, and Promtail for a local test stack.
- Cloudflare Tunnel is now documented and scripted as an admin or limited signaling helper, not as a complete public media path.

## Migration Path

1. Build and validate locally on macOS.
2. Export config and secrets with `./scripts/backup.sh --compress`.
3. Deploy the Linux stack on Hetzner.
4. Re-run load tests against the public Linux endpoint before calling the environment production-ready.
