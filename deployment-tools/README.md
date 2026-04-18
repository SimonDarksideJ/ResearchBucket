# Deployment tools

This folder contains refreshed starter assets for the current LiveKit research.

## Files

- `docker-compose.hetzner.yml` — single-node Linux/Hetzner stack
- `docker-compose.mac.yml` — local Mac stack for routed smoke tests
- `caddy/Caddyfile` — HTTPS reverse proxy for the public Hetzner endpoint
- `livekit/livekit.hetzner.yaml` — Linux/Hetzner LiveKit config template
- `livekit/livekit.mac.yaml` — Mac LiveKit config template for public-edge routing
- `edge/Caddyfile` — public edge HTTPS proxy example for the Mac deployment
- `edge/haproxy.cfg` — public edge TCP forwarding example for the Mac deployment
- `monitoring/docker-compose.monitoring.yml` — Prometheus + Grafana starter stack
- `monitoring/prometheus.yml` — Prometheus scrape config for LiveKit metrics
- `monitoring/grafana/provisioning/datasources/livekit.yml` — Grafana datasource bootstrap

## Notes

- Replace all placeholder domains, IPs, and secrets before use.
- The Hetzner deployment assumes Linux host networking for LiveKit.
- Create a local `certs/` folder before starting LiveKit anywhere that uses embedded TURN/TLS.
- The Mac deployment is intentionally biased toward TCP/TURN/TLS smoke testing.
