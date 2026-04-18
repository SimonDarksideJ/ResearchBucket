# Quick Reference

This page is the short version of the refreshed research. It is opinionated, current-day, and intentionally conservative.

## LiveKit Facts That Change Sizing

- A room must fit on one node. Adding more nodes increases room count and resilience, not the maximum size of a single room.
- CPU and bandwidth are the primary bottlenecks. LiveKit's published benchmark examples use a 16-core compute-optimized host, so small 2 vCPU VMs should not be sized by simple linear math.
- Public browser access needs direct media reachability: HTTPS for signaling, plus routed TCP and UDP ports for WebRTC and TURN/UDP.
- Cloudflare Tunnel is useful for admin access and limited signaling exposure, but it does not replace the required UDP media path.

## Current Hetzner Snapshot

Based on the current Hetzner Cloud site, practical planning assumptions are:

- Shared plans now start at roughly EUR 4-6 per month.
- Dedicated cloud plans start at roughly EUR 16.49 per month.
- 20 TB of traffic is included.
- Additional traffic in the EU is about EUR 1 per TB.

That keeps Hetzner attractive for self-hosted LiveKit compared with AWS, Azure, and GCP, where bandwidth typically dominates cost far earlier.

## Recommended Starting Points

| Use case | Recommended start | Why |
| -------- | ----------------- | --- |
| Local development | Mac local stack | Fast feedback, zero monthly infra cost |
| External integration test | Mac plus routed edge path | Mirrors production behavior without moving the app yet |
| Staging or pilot | Hetzner shared 2 vCPU / 8 GB class VM | Good for one small room and validation work |
| Small production | Hetzner dedicated 4+ vCPU / 16+ GB class VM | More predictable CPU and better headroom |
| Larger production | Multi-node LiveKit plus Redis | Needed for redundancy and many simultaneous rooms |

If you need to promise a capacity number, load-test the exact room pattern first with `lk load-test`.

## Cost Ranges

### Mac test environment

| Component | Typical cost |
| --------- | ------------ |
| macOS host | existing hardware |
| Docker Desktop | free for personal use |
| External SSD | optional one-time purchase |
| Domain | optional |
| Public edge relay VPS | optional, usually low single digits to low teens per month |

### Hetzner production path

| Component | Typical cost |
| --------- | ------------ |
| Shared pilot VM | about EUR 4-12/month |
| Dedicated production VM | from about EUR 16.49/month |
| Domain | about USD 6-15/year |
| DNS | free with Cloudflare or registrar |
| TLS | free with Let's Encrypt via Caddy |

## Ports You Actually Need

Current LiveKit self-hosting guidance means the following ports matter for a single-node VM deployment:

| Port | Purpose |
| ---- | ------- |
| `80/tcp` | ACME challenge and HTTP redirect |
| `443/tcp` | HTTPS and WebSocket signaling |
| `7881/tcp` | ICE over TCP fallback |
| `3478/udp` | TURN/UDP |
| `50000-60000/udp` | WebRTC UDP media range |

The Mac test stack uses `50000-50100/udp` by default to keep the lab footprint smaller.

## DNS Cheat Sheet

For the Linux production host:

```text
Type: A
Name: livekit
Target: YOUR_SERVER_IP
Proxy/CDN: OFF
TTL: Auto
```

For browser-facing LiveKit traffic, keep proxy mode off unless you fully understand the media implications and have preserved the routed UDP path.

## Mac Exposure Modes

### Supported

1. Local-only development on `http://localhost:7880`.
2. Direct home-router port forwarding if your ISP gives you a reachable public IP and you can terminate HTTPS.
3. Public edge relay or VPS with WireGuard plus TCP/UDP forwarding back to the Mac.

### Not a full substitute for routing

1. Cloudflare Tunnel alone.
2. HTTP reverse proxy alone.
3. Any setup that exposes only `443/tcp` while hiding the media UDP path.

## External Services

- Hetzner Cloud: <https://console.hetzner.cloud>
- Cloudflare DNS and Tunnel: <https://developers.cloudflare.com/cloudflare-one/connections/connect-networks/>
- Docker Desktop for Mac: <https://www.docker.com/products/docker-desktop>
- LiveKit self-hosting docs: <https://docs.livekit.io/transport/self-hosting/>

## Read Next

- [Linux-Hosting-Research-LiveKit.md](Linux-Hosting-Research-LiveKit.md)
- [06-hetzner-deployment.md](06-hetzner-deployment.md)
- [07-mac-deployment.md](07-mac-deployment.md)
