# ResearchBucket

A repository for comprehensive research and analysis on hosting infrastructure and deployment solutions.

## 🚀 Quick Start

**[📋 Quick Reference Guide](QUICK-REFERENCE.md)** - Costs, external services, and DNS setup cheat sheet

### For Mac Development (FREE)
```bash
git clone https://github.com/SimonDarksideJ/ResearchBucket.git
cd ResearchBucket/deployment-tools
./scripts/install-mac.sh --storage /Volumes/YourDrive/livekit
```

### For Hetzner Production (~$13/month)
```bash
# After creating server and configuring DNS
./scripts/deploy.sh --platform linux --env production
```

---

## 📚 Research Documents

### Linux Hosting Research for LiveKit Media Server
**[📄 Read the full report](Linux-Hosting-Research-LiveKit.md)**

Comprehensive research on Linux hosting options worldwide with focus on EU deployment for LiveKit media servers.

**Includes:**
- Executive summary and scenario-based recommendations
- Detailed analysis of 10+ hosting providers (Hetzner, Linode, DigitalOcean, Vultr, OVHcloud, AWS, GCP, Azure, UpCloud, Scaleway)
- Comprehensive comparison matrix covering performance, bandwidth, uptime, security, and support
- LiveKit-specific requirements and considerations
- Cost projections and optimization strategies
- Implementation roadmap
- Security considerations including LetsEncrypt support
- Container hosting and Kubernetes options for scalability
- Deals, offers, and long-term benefits analysis

**Top Recommendations:**
- **Best Value for EU**: Hetzner Cloud
- **Best for Containers & Scale**: Linode (Akamai Connected Cloud)
- **Best Developer Experience**: DigitalOcean
- **Best EU Compliance**: Hetzner or OVHcloud

---

## 🚀 Deployment Guides

### Hetzner Cloud Deployment
**[📄 Hetzner Deployment Guide](06-hetzner-deployment.md)**

Complete guide for deploying LiveKit on Hetzner Cloud infrastructure with monitoring and management.

**Features:**
- Server specifications and sizing recommendations
- Network configuration and firewall setup
- Docker-based deployment
- Monitoring stack (Prometheus + Grafana + Loki)
- Security hardening and SSL/TLS configuration
- Backup strategy and maintenance procedures
- Performance optimization for production
- Cost optimization strategies

### Mac Local Deployment
**[📄 Mac Deployment Guide](07-mac-deployment.md)**

End-to-end solution for deploying LiveKit media server on macOS with external storage, monitoring dashboard, and secure public access.

**Features:**
- ✅ Single-command automated installation
- ✅ External storage support (save main drive space)
- ✅ Complete monitoring stack (Grafana + Prometheus + Loki)
- ✅ Web dashboard for health/status monitoring
- ✅ Cloudflare Tunnel for secure public access (no router config)
- ✅ Automated log management and rotation
- ✅ Cross-platform scripts (Mac & Linux compatible)
- ✅ Comprehensive troubleshooting guides

**Quick Start:**
```bash
cd deployment-tools
./scripts/install-mac.sh --storage /Volumes/ExternalDrive/livekit
```

---

## 🛠️ Deployment Tools

**[📁 Deployment Tools Directory](deployment-tools/)**

Automated scripts and configurations for deploying LiveKit on Mac and Linux platforms.

### Available Scripts

| Script | Purpose | Platform |
|--------|---------|----------|
| `install-mac.sh` | Complete Mac installation | macOS |
| `deploy.sh` | Universal deployment (auto-detects) | Mac/Linux |
| `health-check.sh` | Service health monitoring | Mac/Linux |
| `setup-cloudflare-tunnel.sh` | Public access setup | Mac/Linux |
| `backup.sh` | Configuration backup | Mac/Linux |

### Monitoring Stack

Pre-configured monitoring with:
- **Prometheus**: Metrics collection and alerting
- **Grafana**: Visualization dashboards
- **Loki**: Log aggregation and analysis
- **Promtail**: Log shipping
- **Node Exporter**: System metrics
- **cAdvisor**: Container metrics

### Key Features

- 🔄 **Cross-platform**: Same scripts work on Mac and Linux
- 📦 **Containerized**: All services run in Docker
- 📊 **Monitoring**: Real-time dashboards and alerting
- 🔐 **Secure**: Auto-generated credentials, SSL/TLS support
- 🌐 **Public Access**: Cloudflare Tunnel for secure exposure
- 💾 **External Storage**: Support for external drives (Mac)
- 📝 **Log Management**: Automated rotation and aggregation
- 🔧 **Easy Migration**: Export/import configs between environments

### Service Access

After installation:
- LiveKit Server: `http://localhost:7880`
- Grafana Dashboard: `http://localhost:3000`
- Prometheus: `http://localhost:9090`
- Log Viewer: Integrated in Grafana

---

## 📖 Documentation Structure

```
ResearchBucket/
├── README.md                              # This file
├── Linux-Hosting-Research-LiveKit.md      # Hosting provider research
├── 06-hetzner-deployment.md               # Hetzner Cloud guide
├── 07-mac-deployment.md                   # Mac deployment guide
└── deployment-tools/                      # Automated deployment
    ├── README.md                          # Tools documentation
    ├── scripts/                           # Deployment scripts
    │   ├── install-mac.sh                 # Mac installer
    │   ├── deploy.sh                      # Universal deployer
    │   ├── health-check.sh                # Health monitoring
    │   ├── setup-cloudflare-tunnel.sh     # Public access
    │   └── backup.sh                      # Backup utility
    └── monitoring/                        # Monitoring configs
        ├── prometheus/                    # Metrics config
        ├── grafana/                       # Dashboard config
        └── loki/                          # Logs config
```

---

## 🎯 Use Cases

### Development & Testing (Mac)
Deploy LiveKit locally on your Mac for development:
```bash
./deployment-tools/scripts/install-mac.sh
```

### Production (Hetzner Cloud)
Deploy to production infrastructure:
```bash
./deployment-tools/scripts/deploy.sh --platform linux --env production
```

### Hybrid Deployment
Start on Mac, migrate to Hetzner:
1. Develop on Mac with local deployment
2. Export configuration: `./scripts/backup.sh --compress`
3. Import to Hetzner: `./scripts/deploy.sh --import backup.tar.gz`

---

## 🤝 Contributing

Contributions welcome! Areas of focus:
- Additional hosting provider research
- Platform-specific optimizations
- Enhanced monitoring dashboards
- Deployment automation improvements
- Documentation updates

---

## 📝 License

This repository contains research, documentation, and deployment tools for LiveKit media server infrastructure.