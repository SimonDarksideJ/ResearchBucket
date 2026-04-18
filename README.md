# ResearchBucket

Current-day research and deployment guidance for self-hosting LiveKit, with a focus on Hetzner-hosted deployments and a smaller Mac-based test deployment that sits behind an ISP-managed firewall.

## Current conclusions

- LiveKit OSS is still viable for self-hosting, but the old sizing approach of counting participants alone is not accurate enough.
- Sizing must be driven by **tracks, bitrate, CPU, and uplink**, and **each room still has to fit on a single LiveKit node** even in a distributed deployment.
- Upstream still describes LiveKit as primarily **CPU- and bandwidth-bound**, and production deployments should be treated as real-time infrastructure rather than a generic web workload.
- For Hetzner, a **dedicated-vCPU or bare-metal starting point** is the safer choice for production video rooms. Shared-vCPU cloud nodes remain useful for development, smoke tests, and very small pilots.
- A Mac deployment behind an ISP firewall can mirror the Hetzner stack for **functional testing**, but it should not be used for capacity sizing unless you also expose the required UDP media paths through a public edge.

## Validated inputs used for this refresh

This refresh was checked against official LiveKit GitHub-hosted sources that were reachable from this environment:

- LiveKit latest OSS release: **v1.11.0**
- LiveKit install script for Linux
- LiveKit sample configuration and current limit defaults
- LiveKit self-hosting deployment, firewall, distributed, testing, and benchmarking guidance

## Repository contents

- [`docs/hetzner-livekit.md`](docs/hetzner-livekit.md) — refreshed Hetzner deployment, viability, and sizing guidance
- [`docs/mac-test-deployment.md`](docs/mac-test-deployment.md) — mirrored Mac test deployment with public edge routing notes
- [`deployment-tools/README.md`](deployment-tools/README.md) — how to use the refreshed deployment assets
- [`deployment-tools/docker-compose.hetzner.yml`](deployment-tools/docker-compose.hetzner.yml) — single-node Hetzner starter stack
- [`deployment-tools/docker-compose.mac.yml`](deployment-tools/docker-compose.mac.yml) — Mac/local functional-test stack
- [`deployment-tools/livekit/`](deployment-tools/livekit/) — LiveKit configuration templates
- [`deployment-tools/caddy/Caddyfile`](deployment-tools/caddy/Caddyfile) — HTTPS reverse proxy for the public endpoint
- [`deployment-tools/edge/`](deployment-tools/edge/) — public edge examples for the Mac deployment
- [`deployment-tools/monitoring/`](deployment-tools/monitoring/) — Prometheus and Grafana starter assets

## Recommended next step

Start with the Hetzner single-node deployment, run LiveKit connectivity tests plus a short publisher/subscriber load test, and only then finalize node size or decide whether to move to a multi-node cluster or LiveKit Cloud.
