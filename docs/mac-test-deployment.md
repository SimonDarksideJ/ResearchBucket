# Mac test deployment that mirrors Hetzner

## What this setup is for

This setup mirrors the Hetzner deployment closely enough for functional testing, SDK integration checks, and deployment rehearsals.

It is **not** a valid replacement for Hetzner sizing tests unless your public edge also exposes the required UDP media paths.

## Key limitation

A Mac on a consumer or ISP-managed connection usually does not have the same network shape as a public Hetzner server:

- inbound UDP is often blocked or not practically forwardable
- public IP discovery may be wrong from inside NAT
- a plain HTTP reverse proxy is not enough for full WebRTC behavior

Because of that, the realistic pattern is:

1. Run the same LiveKit + Redis stack on the Mac
2. Put a small public edge in front of it
3. Connect the edge to the Mac over WireGuard or Tailscale
4. Forward **HTTPS/WebSocket** and **TCP fallback paths** to the Mac
5. Treat the result as a **smoke-test environment**, not a capacity benchmark

## Recommended architecture

- **Mac host:** runs the stack in [`deployment-tools/docker-compose.mac.yml`](../deployment-tools/docker-compose.mac.yml)
- **Public edge host:** a small public VPS, such as a cheap Hetzner node, running:
  - Caddy for `livekit.example.com` HTTPS/WebSocket proxying
  - HAProxy TCP forwarding for `7881/tcp` and `5349/tcp`
- **Private tunnel:** WireGuard between the edge and the Mac

The included example edge assets are in:

- [`deployment-tools/edge/Caddyfile`](../deployment-tools/edge/Caddyfile)
- [`deployment-tools/edge/haproxy.cfg`](../deployment-tools/edge/haproxy.cfg)

## Important behavior difference from Hetzner

Unless you also forward the UDP media ports from the public edge, remote clients will mostly rely on **ICE/TCP** and **TURN/TLS**. That is acceptable for connectivity testing but it will bias latency and throughput.

That means:

- good for login/join/publish/subscription validation
- good for verifying certs, DNS, tokens, and app wiring
- not good for estimating production room size

## Mac deployment steps

1. Install Docker Desktop or OrbStack.
2. Bring up the local stack:

   ```bash
   docker compose -f deployment-tools/docker-compose.mac.yml up -d
   ```

3. Edit [`deployment-tools/livekit/livekit.mac.yaml`](../deployment-tools/livekit/livekit.mac.yaml):
   - set `rtc.use_external_ip: false`
   - set `rtc.node_ip` to the **public edge IP**
   - set `turn.domain` to the public TURN hostname
   - replace placeholder API keys and secrets
4. Bring up the public edge with the included Caddy and HAProxy examples.
5. Validate remote connectivity with one browser on another network.

## DNS and ports

Create DNS records that point at the **public edge**, not the Mac:

- `livekit.example.com` -> edge public IP
- `turn.example.com` -> edge public IP

Expose on the edge:

- `80/tcp`
- `443/tcp`
- `7881/tcp`
- `5349/tcp`

## What not to do

Do not use this environment to answer questions like:

- how many production participants fit on one node
- whether your UDP media range is large enough
- whether your ISP path is good enough for real-time video at scale

Use the Hetzner deployment for those answers.

## Bottom line

The Mac deployment should now mirror the Hetzner stack operationally, with one major caveat:

**behind an ISP firewall it is a routed test environment, not a trustworthy performance environment.**
