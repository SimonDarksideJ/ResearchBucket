# Deployment Tools

Current deployment and observability assets for the refreshed LiveKit research in this repository.

## Layout

```text
deployment-tools/
├── README.md
├── scripts/
│   ├── deploy.sh
│   ├── install-linux.sh
│   ├── install-mac.sh
│   ├── health-check.sh
│   ├── backup.sh
│   └── setup-cloudflare-tunnel.sh
└── monitoring/
    ├── grafana/
    ├── loki/
    ├── prometheus/
    └── dashboards/
```

## Platform Intent

### Linux Stack

The Linux path is the public-node baseline.

- LiveKit
- Redis
- Caddy for HTTPS termination
- Prometheus
- Grafana
- Loki
- Promtail
- Node Exporter

### macOS Stack

The macOS path is the local test baseline.

- LiveKit
- Redis
- Prometheus
- Grafana
- Loki
- Promtail

Node Exporter is intentionally not part of the macOS stack because Docker Desktop does not present the host like a normal Linux machine.

## Quick Start

### Linux Command

```bash
cd deployment-tools
sudo ./scripts/deploy.sh \
  --platform linux \
  --env production \
  --domain livekit.example.com \
  --email ops@example.com
```

### macOS Command

```bash
cd deployment-tools
./scripts/install-mac.sh
```

If you want external storage and know the mounted drive name, pass `--storage "/Volumes/DriveName/livekit"`.

## Scripts

### `deploy.sh`

Wrapper script that dispatches to the platform-specific installer.

Important options:

- `--platform mac|linux`
- `--env dev|production`
- `--storage PATH`
- `--domain DOMAIN`
- `--email EMAIL`
- `--grafana-pass PASSWORD`
- `--skip-deps`

### `install-linux.sh`

Creates the public Linux stack used by the Hetzner guide.

It writes the deployment under `/opt/livekit`, provisions current config files, and starts Docker Compose.

### `install-mac.sh`

Creates the local test stack on macOS.

It defaults to a safe internal path at `~/livekit-data` and leaves public exposure decisions to either router forwarding or an external edge relay.

### `health-check.sh`

Inspects the local Docker Compose stack, confirms service state, probes the local endpoints, and can emit JSON for automation.

Examples:

```bash
./scripts/health-check.sh
./scripts/health-check.sh --verbose
./scripts/health-check.sh --json
```

### `backup.sh`

Backs up the deployment configuration and local state worth keeping.

Included by default:

- `config/`
- `monitoring/`
- `docker-compose.yml`
- `.env`
- `.credentials`
- `logs/`
- service metadata

Examples:

```bash
./scripts/backup.sh
./scripts/backup.sh --compress
./scripts/backup.sh --compress --encrypt
./scripts/backup.sh --remote user@backup-host
```

### `setup-cloudflare-tunnel.sh`

Admin helper for Cloudflare Tunnel.

This is intentionally scoped to Grafana or limited signaling exposure. It is not presented as a complete public media solution.

Examples:

```bash
./scripts/setup-cloudflare-tunnel.sh --service grafana
./scripts/setup-cloudflare-tunnel.sh --service livekit --hostname livekit-admin.example.com
```

## Monitoring Stack

### Prometheus

Linux baseline scrapes:

- LiveKit metrics
- Prometheus itself
- Node Exporter
- Loki
- Promtail

The macOS installer writes a smaller Prometheus config that removes Linux host metrics.

### Loki

The repo now uses a current single-node filesystem layout with TSDB-backed indexing instead of the older boltdb-shipper plus table-manager shape.

### Promtail

Promtail now uses Docker service discovery through the Docker socket instead of assuming Linux-only container log file paths.

### Grafana

Grafana is provisioned with Prometheus and Loki data sources out of the box. Add dashboard JSON files to `monitoring/dashboards/` if you want auto-provisioned dashboards.

## Security and Exposure Defaults

- Linux binds Grafana and Prometheus to localhost and expects SSH tunnels or VPN access.
- Linux exposes the LiveKit public path only through the intended public ports.
- macOS keeps the stack local by default.
- Cloudflare Tunnel is documented as an admin tool, not as a replacement for LiveKit's UDP media path.

## Related Documents

- [../06-hetzner-deployment.md](../06-hetzner-deployment.md)
- [../07-mac-deployment.md](../07-mac-deployment.md)
- [../Linux-Hosting-Research-LiveKit.md](../Linux-Hosting-Research-LiveKit.md)
