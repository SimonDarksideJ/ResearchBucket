# LiveKit Deployment Quick Reference

## Your Setup Recommendations

Based on your requirements, here's what we recommend:

### For Mac Development

**✅ Use Tailscale** (100% Free, Most Secure)

- **Why**: Perfect for development on your Mac
- **Cost**: $0/year (free for personal use, up to 100 devices)
- **Setup Time**: 5 minutes
- **Access**: From Mac, iPhone, iPad, or any device on your Tailscale network
- **Security**: Private network, not exposed to internet
- **Router Config**: None required

**Quick Start:**
```bash
# 1. Install Tailscale
brew install tailscale

# 2. Start and authenticate
sudo tailscale up

# 3. Get your IP
tailscale ip -4

# 4. Run setup script
cd ResearchBucket/tools/mac-livekit/scripts
./setup.sh --proxy tailscale

# 5. Access from any Tailscale device
# Use the Tailscale IP shown above
```

**Links:**
- Sign up: https://tailscale.com/start
- Download Mac app: https://tailscale.com/download/mac
- Dashboard: https://login.tailscale.com/admin
- Documentation: https://tailscale.com/kb

**Full Guide**: [08-mac-deployment.md](08-mac-deployment.md#option-3-tailscale-recommended-for-mac-development)

---

### For Hetzner Production

**✅ Use Your Existing Domain + DNS** (Just configure A record)

Since you already have a domain and DNS configured, you just need to:

1. **Provision Hetzner Server** → Get your server IP address
2. **Create DNS A Record**: 
   ```
   Type: A
   Name: media (or your subdomain)
   Value: YOUR_HETZNER_IP
   TTL: 300 seconds (or Auto)
   ```
3. **Run deployment commands on server**:
   ```bash
   # Install Docker
   curl -fsSL https://get.docker.com | sh
   
   # Open firewall ports
   ufw allow 80/tcp
   ufw allow 443/tcp
   ufw allow 50000:60000/udp
   
   # Deploy LiveKit with Caddy (automatic SSL)
   # See full guide for Docker Compose config
   ```

**That's it!** Caddy will automatically:
- Obtain SSL certificate from Let's Encrypt
- Configure HTTPS/WSS
- Renew certificates every 90 days

**Cost Breakdown:**
- Hetzner VPS: €4.15/month (~$4.50)
- Domain: Already have ✅
- DNS: Already configured ✅
- SSL: Free (Let's Encrypt via Caddy)
- **Total: ~$4.50/month**

**Setup Time**: ~15 minutes (most is DNS propagation)

**Full Guide**: [06-hetzner-deployment.md](06-hetzner-deployment.md#quick-start-already-have-domain--dns)

---

## Cost Summary

### Mac Development (Tailscale)
- **Monthly**: $0
- **Annual**: $0
- **One-time**: $0
- **Total**: **FREE** ✅

### Hetzner Production
- **Monthly**: $4.50 (VPS)
- **Annual**: $54
- **One-time**: $0 (domain already owned)
- **Total**: **~$54/year** ✅

### Combined Total for Both Environments
- **$54/year** for development + production
- **$4.50/month** ongoing

---

## Quick Links

### Documentation
1. [Mac Deployment Guide](08-mac-deployment.md) - Full Mac setup with monitoring
2. [Hetzner Deployment Guide](06-hetzner-deployment.md) - Production deployment
3. [Cost & Configuration Guide](09-cost-and-configuration-guide.md) - Detailed cost analysis

### Scripts (Mac)
Located in: `tools/mac-livekit/scripts/`

- `setup.sh` - Interactive installation
- `start.sh` - Start all services
- `stop.sh` - Stop all services
- `status.sh` - Check service status
- `logs.sh` - View logs
- `health-check.sh` - Comprehensive health check
- `backup.sh` - Backup configuration

### External Services

**Tailscale (Mac Development)**
- Sign up: https://tailscale.com/start
- Dashboard: https://login.tailscale.com/admin
- Cost: Free (personal use)

**Hetzner (Production)**
- Console: https://console.hetzner.cloud
- Pricing: https://www.hetzner.com/cloud
- Cost: €4.15/month for CX21

---

## Common Commands

### Mac Development

```bash
# Full setup
cd tools/mac-livekit/scripts
./setup.sh

# Check status
./status.sh

# View LiveKit logs
./logs.sh livekit --follow

# Health check
./health-check.sh

# Access monitoring dashboard
open http://localhost:3000

# Get Tailscale IP (to share with team)
tailscale ip -4
```

### Hetzner Production

```bash
# SSH to server
ssh root@YOUR_HETZNER_IP

# Check Docker status
docker compose ps

# View LiveKit logs
docker compose logs -f livekit

# Restart services
docker compose restart

# Check SSL certificate
curl -I https://media.yourdomain.com

# Monitor resources
htop
```

---

## Troubleshooting Quick Tips

### Mac (Tailscale)

**Can't connect to LiveKit:**
```bash
# 1. Check Tailscale is running
tailscale status

# 2. Check LiveKit is running
cd tools/mac-livekit/scripts
./status.sh

# 3. Check firewall
sudo /usr/libexec/ApplicationFirewall/socketfilterfw --getglobalstate

# 4. Test connection
curl http://$(tailscale ip -4):7880/health
```

**Grafana not accessible:**
```bash
# Check if Grafana is running
docker ps | grep grafana

# Check logs
cd tools/mac-livekit/scripts
./logs.sh grafana --tail 50
```

### Hetzner

**DNS not resolving:**
```bash
# Check DNS propagation
nslookup media.yourdomain.com
dig media.yourdomain.com

# Flush DNS cache (if needed)
sudo systemd-resolve --flush-caches
```

**SSL certificate not obtained:**
```bash
# Check Caddy logs
docker compose logs caddy | grep -i error

# Common issues:
# - DNS not propagated yet (wait 5-30 minutes)
# - Port 80 blocked (check firewall)
# - Domain not pointing to correct IP
```

**Can't connect to LiveKit:**
```bash
# 1. Check firewall
ufw status

# 2. Check services
docker compose ps

# 3. Check logs
docker compose logs livekit | tail -50

# 4. Test locally
curl http://localhost:7880/health

# 5. Test externally
curl https://media.yourdomain.com/health
```

---

## Next Steps

### Mac Development (Right Now)

1. **Install Tailscale** (2 minutes)
   ```bash
   brew install tailscale
   sudo tailscale up
   ```

2. **Run setup script** (3 minutes)
   ```bash
   cd tools/mac-livekit/scripts
   ./setup.sh
   # Select Tailscale when prompted
   ```

3. **Access monitoring** (instant)
   ```bash
   open http://localhost:3000
   # Default login: admin / [shown during setup]
   ```

4. **Get connection info** (instant)
   ```bash
   ./status.sh
   # Copy the Tailscale IP and port
   ```

### Hetzner Production (When Ready)

1. **Provision server** (~2 minutes on Hetzner console)
   - Choose CX21 (€4.15/month)
   - Select Ubuntu 22.04
   - Note the IP address

2. **Update DNS** (~5-30 minutes for propagation)
   - Log into your DNS provider
   - Create A record: `media` → `YOUR_HETZNER_IP`
   - Wait for propagation

3. **Deploy LiveKit** (~10 minutes)
   - SSH to server
   - Follow [Hetzner Quick Start](06-hetzner-deployment.md#quick-start-already-have-domain--dns)
   - Docker Compose up
   - Caddy gets SSL automatically

4. **Test connection** (instant)
   ```bash
   curl https://media.yourdomain.com/health
   ```

---

## Support & Documentation

- **Full Documentation**: [docs/livekit-deployment/](.)
- **LiveKit Official**: https://docs.livekit.io
- **Hetzner Docs**: https://docs.hetzner.com
- **Tailscale Docs**: https://tailscale.com/kb

---

## Summary

You're all set with:
- ✅ **$0/year** Mac development setup (Tailscale)
- ✅ **$54/year** production setup (Hetzner with existing domain)
- ✅ Complete automation scripts
- ✅ Monitoring dashboards
- ✅ No router configuration needed
- ✅ SSL certificates automatic
- ✅ Clear troubleshooting guides

**Total investment: 15-20 minutes setup time + $54/year**

Ready to deploy! 🚀
