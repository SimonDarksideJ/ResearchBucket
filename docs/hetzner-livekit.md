# LiveKit on Hetzner: current viability, sizing, and deployment guidance

## Executive summary

LiveKit remains a viable self-hosted option on Hetzner, but the original sizing assumptions need to be corrected:

1. **A room still has to fit on one LiveKit node.** Redis enables clustering and routing, not room splitting.
2. **Participant count alone is not a capacity model.** Tracks, bitrate, and subscription fan-out are what drive forwarding load.
3. **CPU and bandwidth are the hard limits.** Upstream guidance still treats LiveKit as CPU- and bandwidth-bound, and older benchmark guidance explicitly called out 10 Gbps networking as the production target.
4. **Hetzner shared-vCPU nodes are fine for dev and very small pilots, but they are the wrong default for production video workloads.** Use dedicated vCPU or bare metal if the environment matters.

## What changed in the validation

This refresh was checked against current upstream GitHub-hosted sources rather than the earlier research assumptions:

- Latest LiveKit OSS release at review time: **v1.11.0**
- Current Linux installer: `https://get.livekit.io`
- Current sample config still documents these important defaults:
  - `limit.num_tracks`: defaults to **400 tracks in and out per CPU**, up to 8000
  - `limit.bytes_per_sec`: defaults to **1_000_000_000 bytes/sec** per node
- Current deployment guidance still says:
  - Redis is required for distributed mode
  - LiveKit is CPU/bandwidth bound
  - host networking is preferred for Docker on Linux
  - TURN/TLS remains important for difficult networks

Treat those defaults as **guard rails**, not guarantees.

## Corrected sizing model

The old approach was malformed because it was too participant-centric. A better first-pass model is:

```text
Forwarded bitrate ≈ published track bitrate × number of published tracks × number of subscribers
```

Examples:

- 10 participants, all publishing camera video at 1.0 Mbps, all subscribed to everyone else:
  - ~10 published tracks
  - ~90 downtracks
  - rough forwarding load: ~90 Mbps before protocol overhead and retransmissions
- 25 participants, 8 active cameras at 1.2 Mbps, all subscribed:
  - rough forwarding load: ~240 Mbps before overhead
- 50 participants, 16 active camera tracks at 1.2 Mbps, all subscribed:
  - rough forwarding load: ~960 Mbps before overhead

That is why room shape matters more than total user count. A webinar with few publishers is very different from an all-hands meeting where everyone publishes video.

## Practical Hetzner guidance

### Recommended starting points

| Use case | Hetzner guidance | Notes |
| :-- | :-- | :-- |
| Development / lab | 4 vCPU, 8 GB RAM cloud VM | Good for admin/testing, not for hard sizing |
| Small pilot / internal trial | 8 dedicated vCPU, 16 GB RAM | Better baseline for mixed audio/video rooms |
| Small production | 8-16 dedicated vCPU or bare metal | Prefer predictable CPU scheduling and stronger uplink |
| Multiple busy rooms / larger meetings | Multi-node cluster or LiveKit Cloud | A single node becomes the risk point |

### What to avoid

- Treating shared-vCPU cloud instances as the default for important rooms
- Assuming a distributed cluster lets one oversized room span multiple nodes
- Doing sizing without a publisher/subscriber load test
- Treating a TCP-only fallback test as representative of production media quality

## Current single-node Hetzner design

This repository now includes a refreshed Linux-first deployment in [`deployment-tools/docker-compose.hetzner.yml`](../deployment-tools/docker-compose.hetzner.yml).

### Recommended OS baseline

Use a current long-term-support Linux image, with **Ubuntu 24.04 LTS** being the safest default unless your platform standard says otherwise.

### Pinned / tracked components

- LiveKit server: `livekit/livekit-server:v1.11.0`
- Redis: `redis:7-alpine`
- Caddy: `caddy:2-alpine`
- Prometheus: `prom/prometheus:latest`
- Grafana: `grafana/grafana:latest`

The LiveKit version is pinned because sizing guidance should not float unnoticed. The supporting images are left on stable major tracks unless you have an internal image pinning policy.

## Network and DNS requirements

For a public Hetzner deployment, plan for:

- `livekit.example.com` -> public server IP
- `turn.example.com` -> public server IP

Open these ports:

- `80/tcp` for ACME certificate issuance
- `443/tcp` for HTTPS/WebSocket ingress
- `7881/tcp` for ICE/TCP fallback
- `5349/tcp` for TURN/TLS
- `50000-51000/udp` for WebRTC media

Notes:

- The sample range is intentionally narrower than the older `50000-60000` guidance to reduce firewall surface for a starter deployment.
- Expand the UDP range if participant count or concurrency requires it.
- If you collapse TURN/UDP onto `443/udp`, validate that nothing else on the host needs that port.

## Deployment steps

1. Copy the deployment assets from `deployment-tools/` to your server.
2. Replace placeholder domains, API keys, and secrets in:
   - `deployment-tools/livekit/livekit.hetzner.yaml`
   - `deployment-tools/caddy/Caddyfile`
3. Start the stack:

   ```bash
   docker compose -f deployment-tools/docker-compose.hetzner.yml up -d
   ```

4. Confirm containers are healthy:

   ```bash
   docker compose -f deployment-tools/docker-compose.hetzner.yml ps
   docker compose -f deployment-tools/docker-compose.hetzner.yml logs --tail=100 livekit caddy redis
   ```

5. Validate connectivity with:
   - the LiveKit connection tester
   - a real browser join
   - one CLI publisher and several subscribers

## Monitoring and validation

Enable Prometheus metrics on the LiveKit node and scrape them with the included monitoring assets.

Watch at least:

- CPU saturation
- egress bandwidth
- packet loss / retransmission trends
- room count
- participant count
- track count

A deployment is only ready when it survives a short realistic load test without major packet loss, unstable CPU, or heavy TURN/TCP dependency.

## When Hetzner self-hosting is a good fit

Use it when you want:

- controlled cost for low-to-moderate scale
- full ownership of deployment and observability
- internal or regional workloads where you can test the exact room profile

## When to move past the single-node plan

Move to a multi-node cluster or managed LiveKit if any of these are true:

- you need redundancy
- you expect multiple simultaneously busy rooms
- you need larger video rooms with many active publishers
- you do not want to own TURN, firewall, and media-network debugging

## Bottom line

For current-day Hetzner use, the safest path is:

- single-node **only** for development, proof-of-concept, or a well-tested small pilot
- dedicated CPU for anything user-facing
- explicit load validation before finalizing node size
- cluster or managed service once room size, concurrency, or reliability requirements grow
