# Linux Hosting Research for LiveKit

## Scope

This document was refreshed for current-day LiveKit self-hosting on Linux, with Hetzner as the primary target. The earlier research did a good job on raw hosting economics, but it materially underweighted LiveKit's node-bound room limits and networking constraints.

The result of this refresh is simple:

- Hetzner is still a strong fit for self-hosted LiveKit in Europe.
- The first sizing question is not monthly bandwidth. It is whether one room can fit on one node while keeping CPU, packet processing, and UDP connectivity healthy.
- Small shared VMs are useful, but they are not a substitute for a validated production capacity test.

## Revalidated LiveKit Constraints

The following points were cross-checked against current LiveKit self-hosting documentation:

- A room must fit on a single node.
- Distributed mode with Redis increases room count and resilience, but does not make a single room span multiple nodes.
- LiveKit recommends compute-oriented hosts and highlights CPU and bandwidth as the main scaling bounds.
- Public deployments need direct TCP and UDP reachability for signaling, ICE/TCP fallback, and media.
- LiveKit's published benchmark examples are based on a 16-core compute-optimized machine.

Those points invalidate any sizing model that assumes:

- bandwidth alone determines concurrency,
- a cheap node can be linearly extrapolated from the official benchmark host,
- or a pure HTTPS tunnel is sufficient for public browser traffic.

## What Was Wrong With the Earlier Sizing Logic

The earlier guidance leaned too hard on included bandwidth and rough participant-hour math. That missed several LiveKit-specific realities:

1. Track fan-out drives work. A room with many publishers and many subscribers is expensive even when average bitrate is moderate.
2. The room stays on one node. Multi-node growth helps total room count, failover, and operations, but it does not solve an oversized room.
3. UDP reachability matters. If clients fall back from direct UDP to TURN or TCP more often than expected, the effective capacity of the node drops.
4. Shared CPU behaves differently from dedicated CPU. Two shared vCPUs on an inexpensive VM are not equivalent to two dedicated cores on a production-oriented instance.

The updated recommendation is to treat bandwidth as a cost input and CPU plus room shape as the primary sizing inputs.

## Hetzner Reassessment

Hetzner remains the best starting point for EU-first self-hosting for these reasons:

- Included traffic is still generous for the price.
- Overages remain inexpensive compared with hyperscalers.
- The platform now offers a clearer shared-versus-dedicated split, which maps well to lab versus production workloads.
- Germany and Finland remain attractive regions for GDPR-conscious deployments.

Current planning assumptions from Hetzner's public cloud page are roughly:

- shared instances from about EUR 4-6 per month,
- dedicated cloud instances from about EUR 16.49 per month,
- 20 TB included traffic,
- about EUR 1 per TB additional traffic in the EU.

That does not mean the cheapest instance is production-ready. It means the economic base is still excellent.

## Practical Recommendation Matrix

| Scenario | Recommendation | Notes |
| -------- | -------------- | ----- |
| Local development | Mac test stack | Local-first, external access only when a real routed path exists |
| Cheap staging | Hetzner shared 2 vCPU / 8 GB class VM | Suitable for one small room and deployment validation |
| Small production | Hetzner dedicated 4+ vCPU / 16+ GB class VM | Better CPU consistency and safer headroom |
| Many rooms or maintenance-sensitive service | Multi-node LiveKit plus Redis | Improves room count and resilience |
| Large or unpredictable single rooms | Larger dedicated node or bare metal | Solve the room-on-one-node problem directly |

## Interpreting LiveKit Benchmarks Correctly

LiveKit's published examples on a 16-core compute-optimized host are useful directional references:

- audio-heavy large room: 10 active speakers, 3000 listeners,
- interactive 720p meeting: 150 publishers and 150 subscribers,
- 1-to-many livestream: 1 publisher and 3000 subscribers.

Those numbers are not a promise for smaller VMs. They are proof that LiveKit scales when the node shape is appropriate.

For this repository, the right interpretation is:

- do not promise room sizes from a 2 vCPU shared VM without testing,
- do not assume browser users will all connect over ideal UDP paths,
- and do not market a Mac or laptop test environment as production-capable.

## Hetzner Viability by Tier

### Shared VM Tier

Use this for:

- staging,
- CI validation,
- token generation and control-plane checks,
- one small room under controlled conditions.

Do not use it as the default answer for a public production deployment that needs predictable latency or headroom.

### Dedicated Cloud Tier

Use this as the default production starting point when:

- you need better CPU consistency,
- you expect real external users,
- you need monitoring and operational buffer,
- or you do not want noisy-neighbor risk to dominate the experience.

### Dedicated Server or Bare Metal Tier

Use this when:

- one room may grow large,
- recording, ingress, or heavy TURN usage is expected,
- or cost per core matters more than platform convenience.

## Alternatives That Still Make Sense

Hetzner is the value pick, but there are still valid reasons to choose something else.

| Provider posture | When it wins | Tradeoff |
| ---------------- | ------------- | -------- |
| Linode or Akamai | Better global spread and a cleaner path to managed Kubernetes | More expensive for equivalent raw bandwidth |
| DigitalOcean | Strong developer ergonomics and managed services | Higher effective cost for media-heavy workloads |
| OVHcloud | EU ownership and broader dedicated offerings | Tooling and ergonomics can be rougher depending on team preference |
| AWS, Azure, GCP | Enterprise integrations and advanced networking | Bandwidth cost becomes painful quickly for media workloads |

For a European team starting from zero, none of those beat Hetzner on combined price and simplicity unless a managed platform feature is the real requirement.

## Revised Sizing Method

Use this order of operations instead of rough monthly traffic math:

1. Define the dominant room pattern.
2. Define the expected publisher count.
3. Decide whether clients must work from hostile networks.
4. Pick the smallest node class that is credible for that room shape.
5. Run `lk load-test` against the real endpoint.
6. Leave operational headroom.

Applied examples:

- a small interactive meeting behaves differently from a town hall,
- more publishers change CPU faster than more passive listeners,
- and more fallback traffic changes the effective capacity of the node.

## Bottom Line

- Hetzner is still viable and still the default recommendation for EU-first self-hosted LiveKit.
- The corrected production baseline is a dedicated Linux node with direct UDP reachability, Redis in the stack, and observability from day one.
- The corrected lab baseline is a local Mac stack, optionally fronted by a public edge relay when remote browser testing is required.
- Any final concurrency number belongs to a load test, not a spreadsheet.

## Next Documents

- [06-hetzner-deployment.md](06-hetzner-deployment.md)
- [07-mac-deployment.md](07-mac-deployment.md)
- [deployment-tools/README.md](deployment-tools/README.md)
