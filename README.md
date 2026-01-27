# ResearchBucket

A collection of research and documentation for various technologies and deployment strategies.

## Contents

### LiveKit Deployment Documentation

Comprehensive guides for deploying LiveKit Server across various platforms and architectures.

📁 **[LiveKit Deployment Guides](docs/livekit-deployment/)**

**Available Guides:**

- [Executive Summary & Recommendations](docs/livekit-deployment/00-executive-summary.md) - Comparison matrix, cost analysis, and decision framework
- [Linux Host Deployment](docs/livekit-deployment/01-linux-host-deployment.md) - Native deployment with systemd
- [Docker Container Deployment](docs/livekit-deployment/02-docker-container-deployment.md) - Containerized deployment
- [Kubernetes Deployment](docs/livekit-deployment/03-kubernetes-deployment.md) - Production-scale orchestration
- [Cloud-Native Deployment](docs/livekit-deployment/04-cloud-native-deployment.md) - AWS, GCP, and Azure managed services
- [Load Balancing & Auto-Scaling](docs/livekit-deployment/05-load-balancing-autoscaling.md) - Advanced scaling patterns
- [Hetzner Deployment (Docker + Caddy)](docs/livekit-deployment/06-hetzner-deployment.md) - Hetzner-focused quick path to HTTPS/WSS
- [Hetzner Cost & Performance Analysis](docs/livekit-deployment/07-hetzner-cost-performance-analysis.md) - Comprehensive cost/performance breakdown with capacity planning
- [Mac Deployment with Monitoring](docs/livekit-deployment/08-mac-deployment.md) - macOS deployment with external storage, monitoring dashboards, and automated scripts
- [Cost & Configuration Guide](docs/livekit-deployment/09-cost-and-configuration-guide.md) - **NEW!** Complete cost breakdown, external requirements, and setup instructions

**Quick Start:**

1. Start with the [Executive Summary](docs/livekit-deployment/00-executive-summary.md)
2. Use the decision tree to find your ideal approach
3. Follow the specific guide for your chosen deployment method

**Features:**

- ✅ Detailed setup instructions for each approach
- ✅ Cost analysis and comparisons
- ✅ Security best practices
- ✅ Monitoring and logging setup
- ✅ Troubleshooting guides
- ✅ Performance tuning recommendations
- ✅ Real-world use case recommendations
- ✅ **NEW:** Automated deployment scripts for Mac/Linux ([tools/mac-livekit](tools/mac-livekit/))
- ✅ **NEW:** Grafana monitoring dashboards with pre-configured metrics
- ✅ **NEW:** Multiple reverse proxy options (ngrok, Cloudflare, Tailscale)
- ✅ **NEW:** External storage support with automated setup

See the [LiveKit Deployment README](docs/livekit-deployment/README.md) for complete documentation.

### Quick Deploy on Mac

```bash
# Clone the repository
git clone https://github.com/SimonDarksideJ/ResearchBucket.git
cd ResearchBucket

# Run automated setup
./tools/mac-livekit/scripts/setup.sh

# Access monitoring dashboard
open http://localhost:3000
```

See [Mac Deployment Guide](docs/livekit-deployment/08-mac-deployment.md) for detailed instructions.
