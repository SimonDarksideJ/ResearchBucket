# Security and Whitelisting

## Overview

Security is paramount when running local LLM agents, especially when accessing sensitive codebases and potentially connecting to external resources. This guide covers comprehensive security strategies, whitelisting mechanisms, and best practices.

## Threat Model

### Potential Risks

1. **Data Exfiltration**: Model attempting to send code to external servers
2. **Malicious Prompts**: Injection attacks through user input
3. **Unauthorized Access**: Accessing files outside project scope
4. **Network Attacks**: If networked, exposed to LAN threats
5. **Resource Exhaustion**: DOS through excessive requests
6. **Model Poisoning**: Fine-tuning with malicious data

## Multi-Layer Security Architecture

```
┌─────────────────────────────────────────────┐
│ Layer 1: Network Isolation                  │
│ - Firewall rules                            │
│ - No internet for model process             │
│ - VPN/Private network only                  │
└─────────────────────────────────────────────┘
              ↓
┌─────────────────────────────────────────────┐
│ Layer 2: Application Security               │
│ - Authentication & Authorization            │
│ - API key rotation                          │
│ - Rate limiting                             │
└─────────────────────────────────────────────┘
              ↓
┌─────────────────────────────────────────────┐
│ Layer 3: MCP Whitelist Management           │
│ - Resource whitelisting                     │
│ - Domain restrictions                       │
│ - Tool permission system                    │
└─────────────────────────────────────────────┘
              ↓
┌─────────────────────────────────────────────┐
│ Layer 4: File System Controls               │
│ - Directory whitelisting                    │
│ - Read-only mounts                          │
│ - File size limits                          │
└─────────────────────────────────────────────┘
              ↓
┌─────────────────────────────────────────────┐
│ Layer 5: Monitoring & Audit                 │
│ - Request logging                           │
│ - Anomaly detection                         │
│ - Alert system                              │
└─────────────────────────────────────────────┘
```

## Whitelist Management System

### Whitelist Configuration Format

```json
{
  "version": "1.0",
  "last_updated": "2024-01-08T00:00:00Z",
  "approved_by": "admin",
  
  "cloud_apis": {
    "allowed": [
      {
        "provider": "openai",
        "endpoints": ["https://api.openai.com"],
        "purpose": "Complex reasoning fallback",
        "max_monthly_cost": 50.0
      },
      {
        "provider": "anthropic",
        "endpoints": ["https://api.anthropic.com"],
        "purpose": "Code review",
        "max_monthly_cost": 30.0
      }
    ],
    "blocked": ["*"]
  },
  
  "domains": {
    "allowed": [
      "github.com",
      "stackoverflow.com",
      "docs.microsoft.com",
      "reactjs.org",
      "unity.com/docs"
    ],
    "blocked": ["*"],
    "require_approval": ["*.com", "*.net"]
  },
  
  "file_access": {
    "allowed_directories": [
      "/home/user/projects",
      "/home/user/libraries"
    ],
    "readonly_directories": [
      "/usr/share/doc",
      "/opt/documentation"
    ],
    "blocked_directories": [
      "/home/user/.ssh",
      "/home/user/.aws",
      "/etc",
      "/var"
    ],
    "allowed_extensions": [".cs", ".ts", ".tsx", ".js", ".jsx", ".py", ".md", ".json", ".yml"],
    "max_file_size_mb": 10
  },
  
  "mcp_tools": {
    "memory": {"allowed": true, "max_storage_mb": 1000},
    "sequential_thinking": {"allowed": true},
    "filesystem": {"allowed": true, "readonly": false},
    "git": {"allowed": true, "readonly": true},
    "context7": {"allowed": true},
    "web_search": {"allowed": false},
    "code_execution": {"allowed": false}
  },
  
  "pending_approval": [
    {
      "type": "domain",
      "value": "npmjs.com",
      "reason": "Need to check package documentation",
      "requested_at": "2024-01-07T10:30:00Z",
      "requested_by": "agent"
    }
  ]
}
```

### Whitelist Enforcement Implementation

```python
# whitelist_enforcer.py
import json
import re
from typing import Dict, List, Any
from urllib.parse import urlparse

class WhitelistEnforcer:
    def __init__(self, config_path: str):
        self.config_path = config_path
        self.config = self.load_config()
        
    def load_config(self) -> Dict:
        with open(self.config_path, 'r') as f:
            return json.load(f)
    
    def save_config(self):
        with open(self.config_path, 'w') as f:
            json.dump(self.config, f, indent=2)
    
    def check_api_allowed(self, url: str) -> Dict[str, Any]:
        """Check if API endpoint is whitelisted"""
        parsed = urlparse(url)
        domain = parsed.netloc
        
        # Check cloud APIs
        for api in self.config['cloud_apis']['allowed']:
            if any(domain in endpoint for endpoint in api['endpoints']):
                return {
                    'allowed': True,
                    'provider': api['provider'],
                    'purpose': api['purpose']
                }
        
        return {'allowed': False, 'reason': 'API not whitelisted'}
    
    def check_domain_allowed(self, domain: str) -> Dict[str, Any]:
        """Check if domain is whitelisted"""
        allowed = self.config['domains']['allowed']
        
        if domain in allowed:
            return {'allowed': True}
        
        # Check if requires approval
        for pattern in self.config['domains']['require_approval']:
            if self._match_pattern(domain, pattern):
                return {
                    'allowed': False,
                    'requires_approval': True,
                    'message': 'Domain requires manual approval'
                }
        
        return {'allowed': False, 'reason': 'Domain not whitelisted'}
    
    def check_file_access(self, file_path: str, mode: str = 'read') -> Dict[str, Any]:
        """Check if file access is allowed"""
        # Check blocked directories first
        for blocked in self.config['file_access']['blocked_directories']:
            if file_path.startswith(blocked):
                return {
                    'allowed': False,
                    'reason': f'Access to {blocked} is blocked'
                }
        
        # Check allowed directories
        for allowed in self.config['file_access']['allowed_directories']:
            if file_path.startswith(allowed):
                # Check file extension
                if not self._check_extension(file_path):
                    return {
                        'allowed': False,
                        'reason': 'File extension not allowed'
                    }
                
                # Check if write access requested for readonly dir
                if mode == 'write':
                    for readonly in self.config['file_access'].get('readonly_directories', []):
                        if file_path.startswith(readonly):
                            return {
                                'allowed': False,
                                'reason': 'Directory is read-only'
                            }
                
                return {'allowed': True}
        
        return {'allowed': False, 'reason': 'File not in allowed directories'}
    
    def check_mcp_tool(self, tool_name: str) -> Dict[str, Any]:
        """Check if MCP tool is allowed"""
        tools = self.config['mcp_tools']
        
        if tool_name in tools:
            tool_config = tools[tool_name]
            if isinstance(tool_config, dict):
                return {
                    'allowed': tool_config.get('allowed', False),
                    'config': tool_config
                }
            return {'allowed': tool_config}
        
        return {'allowed': False, 'reason': 'Tool not in whitelist'}
    
    def request_approval(self, resource_type: str, value: str, reason: str):
        """Add resource to pending approval"""
        pending = self.config.setdefault('pending_approval', [])
        pending.append({
            'type': resource_type,
            'value': value,
            'reason': reason,
            'requested_at': datetime.now().isoformat(),
            'requested_by': 'agent'
        })
        self.save_config()
        
        # Optionally send notification
        self._send_approval_notification(resource_type, value, reason)
    
    def approve_pending(self, index: int):
        """Approve a pending request"""
        pending = self.config['pending_approval']
        if 0 <= index < len(pending):
            request = pending.pop(index)
            
            # Add to appropriate whitelist
            if request['type'] == 'domain':
                self.config['domains']['allowed'].append(request['value'])
            elif request['type'] == 'api':
                # Add to cloud APIs
                pass
            
            self.save_config()
            return True
        return False
    
    def _match_pattern(self, value: str, pattern: str) -> bool:
        """Match value against wildcard pattern"""
        regex = pattern.replace('.', '\\.').replace('*', '.*')
        return re.match(f'^{regex}$', value) is not None
    
    def _check_extension(self, file_path: str) -> bool:
        """Check if file extension is allowed"""
        allowed_exts = self.config['file_access']['allowed_extensions']
        return any(file_path.endswith(ext) for ext in allowed_exts)
    
    def _send_approval_notification(self, resource_type: str, value: str, reason: str):
        """Send notification for approval request"""
        # Implement notification (email, slack, etc.)
        print(f"APPROVAL NEEDED: {resource_type} - {value}")
        print(f"Reason: {reason}")
```

## Network Security

### Firewall Configuration

#### Linux (iptables)
```bash
#!/bin/bash
# firewall-setup.sh

# Block all outbound except localhost
iptables -A OUTPUT -o lo -j ACCEPT
iptables -A OUTPUT -d 192.168.1.0/24 -j ACCEPT  # Local network
iptables -A OUTPUT -j DROP

# Allow specific whitelisted domains (example)
iptables -A OUTPUT -d github.com -p tcp --dport 443 -j ACCEPT
iptables -A OUTPUT -d api.openai.com -p tcp --dport 443 -j ACCEPT

# Save rules
iptables-save > /etc/iptables/rules.v4
```

#### macOS (pf)
```bash
# /etc/pf.conf
# Block all outgoing except localhost and LAN
block out all
pass out on lo0 all
pass out on en0 to 192.168.1.0/24

# Allow specific domains
pass out on en0 proto tcp to github.com port 443
pass out on en0 proto tcp to api.openai.com port 443
```

#### Windows (PowerShell)
```powershell
# Block Ollama outbound by default
New-NetFirewallRule -DisplayName "Block Ollama Internet" `
  -Direction Outbound `
  -Program "C:\Program Files\Ollama\ollama.exe" `
  -Action Block

# Allow specific IPs/domains
New-NetFirewallRule -DisplayName "Allow GitHub" `
  -Direction Outbound `
  -RemoteAddress 140.82.114.0/24 `
  -Action Allow
```

### VPN Isolation

```bash
# Create isolated VPN for AI traffic only
# Using WireGuard
cat > /etc/wireguard/wg-ai.conf << EOF
[Interface]
PrivateKey = <private-key>
Address = 10.0.0.1/24

[Peer]
# Only internal peers
PublicKey = <peer-public-key>
AllowedIPs = 10.0.0.0/24
EOF

wg-quick up wg-ai
```

## Authentication & Authorization

### JWT-Based Authentication

```python
# auth.py
import jwt
import hashlib
from datetime import datetime, timedelta
from typing import Optional

class AuthManager:
    def __init__(self, secret_key: str):
        self.secret_key = secret_key
        self.users = {}  # In production, use database
        self.revoked_tokens = set()
    
    def create_user(self, username: str, password: str, role: str = 'user'):
        """Create new user"""
        password_hash = hashlib.sha256(password.encode()).hexdigest()
        self.users[username] = {
            'password_hash': password_hash,
            'role': role,
            'created_at': datetime.now()
        }
    
    def authenticate(self, username: str, password: str) -> Optional[str]:
        """Authenticate user and return JWT token"""
        if username not in self.users:
            return None
        
        password_hash = hashlib.sha256(password.encode()).hexdigest()
        if self.users[username]['password_hash'] != password_hash:
            return None
        
        # Generate JWT
        token = jwt.encode(
            {
                'sub': username,
                'role': self.users[username]['role'],
                'exp': datetime.utcnow() + timedelta(days=7)
            },
            self.secret_key,
            algorithm='HS256'
        )
        
        return token
    
    def verify_token(self, token: str) -> Optional[dict]:
        """Verify JWT token"""
        if token in self.revoked_tokens:
            return None
        
        try:
            payload = jwt.decode(token, self.secret_key, algorithms=['HS256'])
            return payload
        except jwt.InvalidTokenError:
            return None
    
    def revoke_token(self, token: str):
        """Revoke a token"""
        self.revoked_tokens.add(token)
    
    def check_permission(self, token: str, resource: str, action: str) -> bool:
        """Check if user has permission for action"""
        payload = self.verify_token(token)
        if not payload:
            return False
        
        role = payload.get('role')
        
        # Define permissions
        permissions = {
            'admin': ['*'],
            'developer': ['read', 'write', 'execute'],
            'viewer': ['read']
        }
        
        allowed_actions = permissions.get(role, [])
        return '*' in allowed_actions or action in allowed_actions
```

## Audit Logging

```python
# audit_logger.py
import logging
import json
from datetime import datetime
from typing import Any, Dict

class AuditLogger:
    def __init__(self, log_file: str):
        self.logger = logging.getLogger('audit')
        self.logger.setLevel(logging.INFO)
        
        handler = logging.FileHandler(log_file)
        formatter = logging.Formatter('%(message)s')
        handler.setFormatter(formatter)
        self.logger.addHandler(handler)
    
    def log_request(self, user: str, action: str, resource: str, 
                   allowed: bool, details: Dict[str, Any] = None):
        """Log a security-relevant action"""
        log_entry = {
            'timestamp': datetime.now().isoformat(),
            'user': user,
            'action': action,
            'resource': resource,
            'allowed': allowed,
            'details': details or {}
        }
        
        self.logger.info(json.dumps(log_entry))
    
    def log_api_call(self, user: str, endpoint: str, cost: float):
        """Log API call for cost tracking"""
        self.log_request(
            user, 'api_call', endpoint, True,
            {'cost': cost, 'type': 'external_api'}
        )
    
    def log_file_access(self, user: str, file_path: str, mode: str, allowed: bool):
        """Log file access attempt"""
        self.log_request(
            user, 'file_access', file_path, allowed,
            {'mode': mode}
        )
    
    def log_security_event(self, event_type: str, details: Dict[str, Any]):
        """Log security event"""
        log_entry = {
            'timestamp': datetime.now().isoformat(),
            'event_type': event_type,
            'severity': 'SECURITY',
            'details': details
        }
        
        self.logger.warning(json.dumps(log_entry))
```

## Anomaly Detection

```python
# anomaly_detector.py
from collections import defaultdict, deque
from datetime import datetime, timedelta
import statistics

class AnomalyDetector:
    def __init__(self):
        self.request_history = defaultdict(lambda: deque(maxlen=1000))
        self.alert_threshold = {
            'requests_per_minute': 100,
            'failed_auth_attempts': 5,
            'unique_ips_per_hour': 10,
            'data_transfer_mb': 1000
        }
    
    def record_request(self, user: str, ip: str, endpoint: str, 
                      data_size: int, success: bool):
        """Record request for anomaly detection"""
        self.request_history[user].append({
            'timestamp': datetime.now(),
            'ip': ip,
            'endpoint': endpoint,
            'data_size': data_size,
            'success': success
        })
        
        # Check for anomalies
        self.check_anomalies(user)
    
    def check_anomalies(self, user: str):
        """Check for suspicious patterns"""
        history = list(self.request_history[user])
        
        if not history:
            return
        
        # Check request rate
        recent = [r for r in history 
                 if datetime.now() - r['timestamp'] < timedelta(minutes=1)]
        
        if len(recent) > self.alert_threshold['requests_per_minute']:
            self._alert('high_request_rate', user, len(recent))
        
        # Check failed auth attempts
        recent_failed = [r for r in recent if not r['success']]
        if len(recent_failed) >= self.alert_threshold['failed_auth_attempts']:
            self._alert('failed_auth_attempts', user, len(recent_failed))
        
        # Check data transfer
        recent_hour = [r for r in history 
                      if datetime.now() - r['timestamp'] < timedelta(hours=1)]
        total_data = sum(r['data_size'] for r in recent_hour) / (1024 * 1024)  # MB
        
        if total_data > self.alert_threshold['data_transfer_mb']:
            self._alert('excessive_data_transfer', user, f"{total_data:.2f}MB")
    
    def _alert(self, alert_type: str, user: str, value: Any):
        """Send security alert"""
        print(f"🚨 SECURITY ALERT: {alert_type}")
        print(f"   User: {user}")
        print(f"   Value: {value}")
        # In production: send email, slack, PagerDuty, etc.
```

## Best Practices

1. **Defense in Depth**: Multiple security layers
2. **Least Privilege**: Minimal permissions by default
3. **Zero Trust**: Verify every request
4. **Audit Everything**: Comprehensive logging
5. **Regular Updates**: Keep whitelist current
6. **Incident Response**: Have a plan for breaches
7. **Regular Reviews**: Weekly security audits
8. **User Education**: Train users on security

## Security Checklist

- [ ] Network isolation configured
- [ ] Firewall rules in place
- [ ] Whitelist configuration created
- [ ] Authentication system implemented
- [ ] Authorization checks added
- [ ] Audit logging enabled
- [ ] Anomaly detection active
- [ ] File access controls set
- [ ] API rate limiting configured
- [ ] Regular backups scheduled
- [ ] Incident response plan documented
- [ ] Security monitoring dashboard created

## Conclusion

Security for local LLM agents requires a multi-layered approach combining network isolation, whitelist management, authentication, and monitoring. The goal is to provide a secure environment that prevents data exfiltration while allowing productive work.

## Next Steps

- [Hardware Requirements](./hardware.md)
- [Containerization](./containers.md)
- [Monitoring Setup](../comparisons/resources-matrix.md)
