# LiveKit Kubernetes Deployment Guide

## Overview

This guide covers deploying LiveKit Server on Kubernetes for production-scale, highly available, and auto-scaling deployments. Kubernetes provides orchestration, self-healing, and advanced deployment strategies.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Installation Steps](#installation-steps)
3. [Configuration](#configuration)
4. [Cluster Management](#cluster-management)
5. [Monitoring & Logging](#monitoring--logging)
6. [Security Considerations](#security-considerations)
7. [Pros and Cons](#pros-and-cons)
8. [Administration Patterns](#administration-patterns)
9. [Gotchas and Troubleshooting](#gotchas-and-troubleshooting)

## Prerequisites

- Kubernetes cluster 1.24+ (EKS, GKE, AKS, or self-managed)
- kubectl CLI configured
- Helm 3.0+ (recommended)
- Minimum cluster resources:
  - 3 worker nodes (production: 5+)
  - 8GB RAM per node minimum
  - Load balancer support
- Storage class for persistent volumes
- Ingress controller (nginx, traefik, or cloud provider)
- Certificate manager (cert-manager recommended)
- Basic Kubernetes knowledge

## Installation Steps

### Method 1: Using Helm Chart (Recommended)

#### Step 1: Add LiveKit Helm Repository

```bash
# Add LiveKit Helm repo
helm repo add livekit https://helm.livekit.io

# Update repositories
helm repo update

# Search for available charts
helm search repo livekit
```

#### Step 2: Create Namespace

```bash
# Create dedicated namespace
kubectl create namespace livekit

# Set as default context (optional)
kubectl config set-context --current --namespace=livekit
```

#### Step 3: Create Values File

Create `livekit-values.yaml`:

```yaml
# LiveKit Helm values
replicaCount: 3

image:
  repository: livekit/livekit-server
  tag: "v1.5.3"
  pullPolicy: IfNotPresent

# Service configuration
service:
  type: LoadBalancer
  annotations:
    # For AWS
    service.beta.kubernetes.io/aws-load-balancer-type: "nlb"
    # For GCP
    # cloud.google.com/load-balancer-type: "Internal"
  
  http:
    port: 7880
    targetPort: 7880
  
  rtc:
    # UDP port range
    portRangeStart: 50000
    portRangeEnd: 50100  # 100 ports for load balancing

# Resource limits
resources:
  limits:
    cpu: 4000m
    memory: 8Gi
  requests:
    cpu: 2000m
    memory: 4Gi

# Horizontal Pod Autoscaler
autoscaling:
  enabled: true
  minReplicas: 3
  maxReplicas: 10
  targetCPUUtilizationPercentage: 70
  targetMemoryUtilizationPercentage: 80

# Pod disruption budget
podDisruptionBudget:
  enabled: true
  minAvailable: 2

# LiveKit configuration
livekit:
  # API keys (use secrets in production)
  keys:
    APIKey1: <your-api-key>
    APISecret1: <your-api-secret>
  
  config:
    port: 7880
    
    rtc:
      port_range_start: 50000
      port_range_end: 50100
      use_ice_lite: true
      tcp_port: 7881
      udp_port: 7882
    
    redis:
      # Use Redis for multi-instance coordination
      address: "redis-master.livekit.svc.cluster.local:6379"
      db: 0
    
    room:
      auto_create: true
      empty_timeout: 300
      max_participants: 100
    
    logging:
      level: info
      json: true
    
    # Node IP will be set to pod IP automatically
    # node_ip: auto-detected

# Redis subchart (for session management)
redis:
  enabled: true
  architecture: standalone
  auth:
    enabled: false
  master:
    persistence:
      enabled: true
      size: 8Gi

# Ingress configuration
ingress:
  enabled: true
  className: nginx
  annotations:
    cert-manager.io/cluster-issuer: letsencrypt-prod
    nginx.ingress.kubernetes.io/ssl-redirect: "true"
  hosts:
    - host: livekit.yourdomain.com
      paths:
        - path: /
          pathType: Prefix
  tls:
    - secretName: livekit-tls
      hosts:
        - livekit.yourdomain.com

# Monitoring
metrics:
  enabled: true
  serviceMonitor:
    enabled: true
    interval: 30s

# Pod security
podSecurityContext:
  runAsNonRoot: true
  runAsUser: 1000
  fsGroup: 1000
  seccompProfile:
    type: RuntimeDefault

securityContext:
  allowPrivilegeEscalation: false
  capabilities:
    drop:
      - ALL
  readOnlyRootFilesystem: true

# Node affinity (spread across zones)
affinity:
  podAntiAffinity:
    preferredDuringSchedulingIgnoredDuringExecution:
      - weight: 100
        podAffinityTerm:
          labelSelector:
            matchExpressions:
              - key: app.kubernetes.io/name
                operator: In
                values:
                  - livekit
          topologyKey: topology.kubernetes.io/zone
```

#### Step 4: Install with Helm

```bash
# Install LiveKit
helm install livekit livekit/livekit-server \
  --namespace livekit \
  --values livekit-values.yaml

# Check deployment status
kubectl get pods -n livekit -w

# Check services
kubectl get svc -n livekit

# Get external IP
kubectl get svc livekit -n livekit -o jsonpath='{.status.loadBalancer.ingress[0].ip}'
```

### Method 2: Manual Kubernetes Manifests

#### ConfigMap for LiveKit Configuration

Create `01-configmap.yaml`:

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: livekit-config
  namespace: livekit
data:
  config.yaml: |
    port: 7880
    bind_addresses:
      - "0.0.0.0"
    
    rtc:
      port_range_start: 50000
      port_range_end: 50100
      use_ice_lite: true
      tcp_port: 7881
      udp_port: 7882
    
    redis:
      address: redis-service:6379
      db: 0
    
    keys:
      # API keys - use Secrets in production
      APIKey1: ${LIVEKIT_API_KEY}
      APISecret1: ${LIVEKIT_API_SECRET}
    
    room:
      auto_create: true
      empty_timeout: 300
      max_participants: 100
      departure_timeout: 20
    
    logging:
      level: info
      json: true
    
    # Region will be set via environment
    region: ${REGION}
```

#### Secret for API Keys

Create `02-secret.yaml`:

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: livekit-secrets
  namespace: livekit
type: Opaque
stringData:
  api-key: "your-api-key-here"
  api-secret: "your-api-secret-here"
```

#### Deployment

Create `03-deployment.yaml`:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: livekit-server
  namespace: livekit
  labels:
    app: livekit-server
spec:
  replicas: 3
  strategy:
    type: RollingUpdate
    rollingUpdate:
      maxSurge: 1
      maxUnavailable: 0
  selector:
    matchLabels:
      app: livekit-server
  template:
    metadata:
      labels:
        app: livekit-server
      annotations:
        prometheus.io/scrape: "true"
        prometheus.io/port: "7880"
        prometheus.io/path: "/metrics"
    spec:
      serviceAccountName: livekit-sa
      
      securityContext:
        runAsNonRoot: true
        runAsUser: 1000
        fsGroup: 1000
      
      containers:
      - name: livekit-server
        image: livekit/livekit-server:v1.5.3
        imagePullPolicy: IfNotPresent
        
        ports:
        - name: http
          containerPort: 7880
          protocol: TCP
        - name: rtc-tcp
          containerPort: 7881
          protocol: TCP
        - name: rtc-udp
          containerPort: 7882
          protocol: UDP
        - name: rtc-start
          containerPort: 50000
          protocol: UDP
        
        env:
        - name: LIVEKIT_API_KEY
          valueFrom:
            secretKeyRef:
              name: livekit-secrets
              key: api-key
        - name: LIVEKIT_API_SECRET
          valueFrom:
            secretKeyRef:
              name: livekit-secrets
              key: api-secret
        - name: REGION
          value: "us-west-2"
        - name: LIVEKIT_NODE_IP
          valueFrom:
            fieldRef:
              fieldPath: status.podIP
        
        volumeMounts:
        - name: config
          mountPath: /config
          readOnly: true
        - name: tmp
          mountPath: /tmp
        
        args:
        - --config
        - /config/config.yaml
        
        resources:
          requests:
            cpu: 2000m
            memory: 4Gi
          limits:
            cpu: 4000m
            memory: 8Gi
        
        livenessProbe:
          httpGet:
            path: /health
            port: 7880
          initialDelaySeconds: 30
          periodSeconds: 10
          timeoutSeconds: 5
          failureThreshold: 3
        
        readinessProbe:
          httpGet:
            path: /health
            port: 7880
          initialDelaySeconds: 10
          periodSeconds: 5
          timeoutSeconds: 3
          failureThreshold: 2
        
        securityContext:
          allowPrivilegeEscalation: false
          capabilities:
            drop:
            - ALL
          readOnlyRootFilesystem: true
      
      volumes:
      - name: config
        configMap:
          name: livekit-config
      - name: tmp
        emptyDir: {}
      
      affinity:
        podAntiAffinity:
          preferredDuringSchedulingIgnoredDuringExecution:
          - weight: 100
            podAffinityTerm:
              labelSelector:
                matchExpressions:
                - key: app
                  operator: In
                  values:
                  - livekit-server
              topologyKey: kubernetes.io/hostname
```

#### Service

Create `04-service.yaml`:

```yaml
apiVersion: v1
kind: Service
metadata:
  name: livekit-service
  namespace: livekit
  annotations:
    service.beta.kubernetes.io/aws-load-balancer-type: "nlb"
    service.beta.kubernetes.io/aws-load-balancer-cross-zone-load-balancing-enabled: "true"
spec:
  type: LoadBalancer
  selector:
    app: livekit-server
  ports:
  - name: http
    port: 7880
    targetPort: 7880
    protocol: TCP
  - name: rtc-tcp
    port: 7881
    targetPort: 7881
    protocol: TCP
  - name: rtc-udp
    port: 7882
    targetPort: 7882
    protocol: UDP
  # UDP port range for media
  - name: media-50000
    port: 50000
    targetPort: 50000
    protocol: UDP
  # Add more ports as needed (50001-50100)
  sessionAffinity: ClientIP
  sessionAffinityConfig:
    clientIP:
      timeoutSeconds: 10800  # 3 hours
---
apiVersion: v1
kind: Service
metadata:
  name: livekit-headless
  namespace: livekit
spec:
  clusterIP: None
  selector:
    app: livekit-server
  ports:
  - name: http
    port: 7880
    targetPort: 7880
```

#### HorizontalPodAutoscaler

Create `05-hpa.yaml`:

```yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: livekit-hpa
  namespace: livekit
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: livekit-server
  minReplicas: 3
  maxReplicas: 10
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 70
  - type: Resource
    resource:
      name: memory
      target:
        type: Utilization
        averageUtilization: 80
  behavior:
    scaleDown:
      stabilizationWindowSeconds: 300
      policies:
      - type: Percent
        value: 50
        periodSeconds: 60
    scaleUp:
      stabilizationWindowSeconds: 0
      policies:
      - type: Percent
        value: 100
        periodSeconds: 15
```

#### PodDisruptionBudget

Create `06-pdb.yaml`:

```yaml
apiVersion: policy/v1
kind: PodDisruptionBudget
metadata:
  name: livekit-pdb
  namespace: livekit
spec:
  minAvailable: 2
  selector:
    matchLabels:
      app: livekit-server
```

#### Apply Manifests

```bash
# Create namespace
kubectl create namespace livekit

# Apply all manifests
kubectl apply -f 01-configmap.yaml
kubectl apply -f 02-secret.yaml
kubectl apply -f 03-deployment.yaml
kubectl apply -f 04-service.yaml
kubectl apply -f 05-hpa.yaml
kubectl apply -f 06-pdb.yaml

# Verify deployment
kubectl get all -n livekit
```

### Method 3: Kustomize Deployment

Create directory structure:
```
k8s/
├── base/
│   ├── kustomization.yaml
│   ├── deployment.yaml
│   ├── service.yaml
│   └── configmap.yaml
└── overlays/
    ├── production/
    │   ├── kustomization.yaml
    │   └── replicas.yaml
    └── staging/
        ├── kustomization.yaml
        └── replicas.yaml
```

`base/kustomization.yaml`:
```yaml
apiVersion: kustomize.config.k8s.io/v1beta1
kind: Kustomization

namespace: livekit

resources:
  - deployment.yaml
  - service.yaml
  - configmap.yaml

commonLabels:
  app: livekit-server
  managed-by: kustomize
```

`overlays/production/kustomization.yaml`:
```yaml
apiVersion: kustomize.config.k8s.io/v1beta1
kind: Kustomization

bases:
  - ../../base

replicas:
  - name: livekit-server
    count: 5

patchesStrategicMerge:
  - replicas.yaml
```

Deploy with:
```bash
# Production
kubectl apply -k overlays/production/

# Staging
kubectl apply -k overlays/staging/
```

## Configuration

### Redis for Multi-Instance Coordination

Deploy Redis for session sharing:

```yaml
apiVersion: apps/v1
kind: StatefulSet
metadata:
  name: redis
  namespace: livekit
spec:
  serviceName: redis-service
  replicas: 1
  selector:
    matchLabels:
      app: redis
  template:
    metadata:
      labels:
        app: redis
    spec:
      containers:
      - name: redis
        image: redis:7-alpine
        ports:
        - containerPort: 6379
        volumeMounts:
        - name: redis-data
          mountPath: /data
        command:
        - redis-server
        - --appendonly
        - "yes"
  volumeClaimTemplates:
  - metadata:
      name: redis-data
    spec:
      accessModes: ["ReadWriteOnce"]
      resources:
        requests:
          storage: 10Gi
---
apiVersion: v1
kind: Service
metadata:
  name: redis-service
  namespace: livekit
spec:
  selector:
    app: redis
  ports:
  - port: 6379
    targetPort: 6379
  clusterIP: None
```

### Ingress Configuration

Install nginx-ingress controller (if not present):

```bash
helm repo add ingress-nginx https://kubernetes.github.io/ingress-nginx
helm install ingress-nginx ingress-nginx/ingress-nginx \
  --namespace ingress-nginx \
  --create-namespace
```

Create Ingress:

```yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: livekit-ingress
  namespace: livekit
  annotations:
    cert-manager.io/cluster-issuer: letsencrypt-prod
    nginx.ingress.kubernetes.io/ssl-redirect: "true"
    nginx.ingress.kubernetes.io/websocket-services: livekit-service
    nginx.ingress.kubernetes.io/proxy-read-timeout: "3600"
    nginx.ingress.kubernetes.io/proxy-send-timeout: "3600"
spec:
  ingressClassName: nginx
  tls:
  - hosts:
    - livekit.yourdomain.com
    secretName: livekit-tls
  rules:
  - host: livekit.yourdomain.com
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: livekit-service
            port:
              number: 7880
```

### cert-manager for SSL

Install cert-manager:

```bash
kubectl apply -f https://github.com/cert-manager/cert-manager/releases/download/v1.13.0/cert-manager.yaml
```

Create ClusterIssuer:

```yaml
apiVersion: cert-manager.io/v1
kind: ClusterIssuer
metadata:
  name: letsencrypt-prod
spec:
  acme:
    server: https://acme-v02.api.letsencrypt.org/directory
    email: admin@yourdomain.com
    privateKeySecretRef:
      name: letsencrypt-prod
    solvers:
    - http01:
        ingress:
          class: nginx
```

## Cluster Management

### Scaling Operations

```bash
# Manual scaling
kubectl scale deployment livekit-server -n livekit --replicas=5

# Check HPA status
kubectl get hpa -n livekit

# View HPA events
kubectl describe hpa livekit-hpa -n livekit

# Temporary disable autoscaling
kubectl patch hpa livekit-hpa -n livekit -p '{"spec":{"minReplicas":3,"maxReplicas":3}}'
```

### Rolling Updates

```bash
# Update image
kubectl set image deployment/livekit-server \
  livekit-server=livekit/livekit-server:v1.5.4 \
  -n livekit

# Check rollout status
kubectl rollout status deployment/livekit-server -n livekit

# Pause rollout
kubectl rollout pause deployment/livekit-server -n livekit

# Resume rollout
kubectl rollout resume deployment/livekit-server -n livekit

# Rollback to previous version
kubectl rollout undo deployment/livekit-server -n livekit

# Rollback to specific revision
kubectl rollout undo deployment/livekit-server -n livekit --to-revision=2

# View rollout history
kubectl rollout history deployment/livekit-server -n livekit
```

### Maintenance Mode

```bash
# Drain node for maintenance
kubectl drain node-1 --ignore-daemonsets --delete-emptydir-data

# Cordon node (prevent new pods)
kubectl cordon node-1

# Uncordon node
kubectl uncordon node-1
```

## Monitoring & Logging

### Prometheus and Grafana Setup

Install kube-prometheus-stack:

```bash
helm repo add prometheus-community https://prometheus-community.github.io/helm-charts
helm install prometheus prometheus-community/kube-prometheus-stack \
  --namespace monitoring \
  --create-namespace
```

Create ServiceMonitor:

```yaml
apiVersion: monitoring.coreos.com/v1
kind: ServiceMonitor
metadata:
  name: livekit-metrics
  namespace: livekit
spec:
  selector:
    matchLabels:
      app: livekit-server
  endpoints:
  - port: http
    path: /metrics
    interval: 30s
```

### Logging with EFK Stack

Deploy Elasticsearch, Fluentd, Kibana:

```bash
# Add elastic helm repo
helm repo add elastic https://helm.elastic.co

# Install Elasticsearch
helm install elasticsearch elastic/elasticsearch \
  --namespace logging \
  --create-namespace

# Install Kibana
helm install kibana elastic/kibana \
  --namespace logging

# Install Fluentd DaemonSet
kubectl apply -f https://raw.githubusercontent.com/fluent/fluentd-kubernetes-daemonset/master/fluentd-daemonset-elasticsearch.yaml
```

### Custom Metrics for Autoscaling

Create custom metrics:

```yaml
apiVersion: v1
kind: Service
metadata:
  name: livekit-metrics
  namespace: livekit
  labels:
    app: livekit-server
spec:
  ports:
  - port: 7880
    name: metrics
  selector:
    app: livekit-server
---
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: livekit-hpa-custom
  namespace: livekit
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: livekit-server
  minReplicas: 3
  maxReplicas: 20
  metrics:
  - type: Pods
    pods:
      metric:
        name: active_rooms
      target:
        type: AverageValue
        averageValue: "10"
```

### Distributed Tracing

Install Jaeger:

```bash
kubectl create namespace observability
kubectl create -f https://github.com/jaegertracing/jaeger-operator/releases/download/v1.49.0/jaeger-operator.yaml -n observability

# Create Jaeger instance
cat <<EOF | kubectl apply -f -
apiVersion: jaegertracing.io/v1
kind: Jaeger
metadata:
  name: livekit-jaeger
  namespace: livekit
spec:
  strategy: production
  storage:
    type: elasticsearch
    options:
      es:
        server-urls: http://elasticsearch:9200
EOF
```

## Security Considerations

### RBAC Configuration

Create ServiceAccount and RBAC:

```yaml
apiVersion: v1
kind: ServiceAccount
metadata:
  name: livekit-sa
  namespace: livekit
---
apiVersion: rbac.authorization.k8s.io/v1
kind: Role
metadata:
  name: livekit-role
  namespace: livekit
rules:
- apiGroups: [""]
  resources: ["pods", "services"]
  verbs: ["get", "list", "watch"]
---
apiVersion: rbac.authorization.k8s.io/v1
kind: RoleBinding
metadata:
  name: livekit-rolebinding
  namespace: livekit
subjects:
- kind: ServiceAccount
  name: livekit-sa
roleRef:
  kind: Role
  name: livekit-role
  apiGroup: rbac.authorization.k8s.io
```

### Network Policies

```yaml
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: livekit-netpol
  namespace: livekit
spec:
  podSelector:
    matchLabels:
      app: livekit-server
  policyTypes:
  - Ingress
  - Egress
  ingress:
  - from:
    - namespaceSelector:
        matchLabels:
          name: ingress-nginx
    ports:
    - protocol: TCP
      port: 7880
  - from:
    - podSelector:
        matchLabels:
          app: livekit-server
    ports:
    - protocol: TCP
      port: 7880
  egress:
  - to:
    - podSelector:
        matchLabels:
          app: redis
    ports:
    - protocol: TCP
      port: 6379
  - to:
    - namespaceSelector: {}
    ports:
    - protocol: UDP
      port: 53
  - to:
    - podSelector: {}
```

### Pod Security Standards

```yaml
apiVersion: v1
kind: Namespace
metadata:
  name: livekit
  labels:
    pod-security.kubernetes.io/enforce: restricted
    pod-security.kubernetes.io/audit: restricted
    pod-security.kubernetes.io/warn: restricted
```

### Secrets Management with External Secrets

```bash
# Install External Secrets Operator
helm repo add external-secrets https://charts.external-secrets.io
helm install external-secrets external-secrets/external-secrets \
  --namespace external-secrets-system \
  --create-namespace
```

Create SecretStore and ExternalSecret:

```yaml
apiVersion: external-secrets.io/v1beta1
kind: SecretStore
metadata:
  name: aws-secretsmanager
  namespace: livekit
spec:
  provider:
    aws:
      service: SecretsManager
      region: us-west-2
      auth:
        jwt:
          serviceAccountRef:
            name: livekit-sa
---
apiVersion: external-secrets.io/v1beta1
kind: ExternalSecret
metadata:
  name: livekit-api-keys
  namespace: livekit
spec:
  refreshInterval: 1h
  secretStoreRef:
    name: aws-secretsmanager
    kind: SecretStore
  target:
    name: livekit-secrets
    creationPolicy: Owner
  data:
  - secretKey: api-key
    remoteRef:
      key: livekit/api-key
  - secretKey: api-secret
    remoteRef:
      key: livekit/api-secret
```

## Pros and Cons

### Pros ✅

1. **Auto-scaling**: Automatic horizontal and vertical scaling
2. **High Availability**: Multi-zone deployment with self-healing
3. **Load Balancing**: Built-in load distribution
4. **Rolling Updates**: Zero-downtime deployments
5. **Self-Healing**: Automatic pod restart on failure
6. **Resource Management**: Efficient resource utilization
7. **Service Discovery**: Built-in DNS and service mesh
8. **Declarative**: Infrastructure as code
9. **Monitoring**: Rich ecosystem of monitoring tools
10. **Cloud Agnostic**: Portable across cloud providers
11. **Scalability**: Handle thousands of concurrent users
12. **Automation**: CI/CD integration for automated deployments

### Cons ❌

1. **Complexity**: Steep learning curve
2. **Operational Overhead**: Requires Kubernetes expertise
3. **Cost**: Higher infrastructure costs
4. **Resource Usage**: Control plane overhead
5. **Debugging**: More complex troubleshooting
6. **Networking**: Complex network configuration
7. **Storage**: Persistent storage complexity
8. **UDP Limitations**: LoadBalancer limitations with UDP
9. **Latency**: Small additional latency from load balancing
10. **Over-Engineering**: May be overkill for small deployments

## Administration Patterns

### Health Monitoring Script

```bash
#!/bin/bash
# k8s-health-check.sh

echo "=== LiveKit Kubernetes Health Check ==="
echo "Date: $(date)"
echo ""

# Pod status
echo "Pod Status:"
kubectl get pods -n livekit -l app=livekit-server
echo ""

# Service endpoints
echo "Service Endpoints:"
kubectl get endpoints -n livekit
echo ""

# HPA status
echo "Autoscaler Status:"
kubectl get hpa -n livekit
echo ""

# Resource usage
echo "Resource Usage:"
kubectl top pods -n livekit -l app=livekit-server
echo ""

# Recent events
echo "Recent Events:"
kubectl get events -n livekit --sort-by='.lastTimestamp' | tail -10
```

### Graceful Scaling Script

```bash
#!/bin/bash
# graceful-scale.sh

NAMESPACE="livekit"
DEPLOYMENT="livekit-server"
TARGET_REPLICAS=$1

if [ -z "$TARGET_REPLICAS" ]; then
    echo "Usage: $0 <target-replicas>"
    exit 1
fi

CURRENT_REPLICAS=$(kubectl get deployment $DEPLOYMENT -n $NAMESPACE -o jsonpath='{.spec.replicas}')

echo "Current replicas: $CURRENT_REPLICAS"
echo "Target replicas: $TARGET_REPLICAS"

if [ "$TARGET_REPLICAS" -lt "$CURRENT_REPLICAS" ]; then
    echo "Scaling down - checking for active rooms..."
    
    # Get pod IPs
    PODS=$(kubectl get pods -n $NAMESPACE -l app=livekit-server -o jsonpath='{.items[*].status.podIP}')
    
    for POD_IP in $PODS; do
        ROOMS=$(curl -s http://$POD_IP:7880/rooms | jq '.rooms | length')
        echo "Pod $POD_IP has $ROOMS active rooms"
    done
    
    read -p "Proceed with scale down? (y/n) " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        exit 1
    fi
fi

kubectl scale deployment $DEPLOYMENT -n $NAMESPACE --replicas=$TARGET_REPLICAS
echo "Scaling initiated"
```

### Backup and Restore

```bash
#!/bin/bash
# backup-livekit-k8s.sh

NAMESPACE="livekit"
BACKUP_DIR="/backup/livekit-k8s"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)

mkdir -p $BACKUP_DIR

# Backup all resources
kubectl get all -n $NAMESPACE -o yaml > "$BACKUP_DIR/all-resources-$TIMESTAMP.yaml"

# Backup configmaps
kubectl get configmap -n $NAMESPACE -o yaml > "$BACKUP_DIR/configmaps-$TIMESTAMP.yaml"

# Backup secrets (be careful with these!)
kubectl get secrets -n $NAMESPACE -o yaml > "$BACKUP_DIR/secrets-$TIMESTAMP.yaml"

# Backup persistent volume claims
kubectl get pvc -n $NAMESPACE -o yaml > "$BACKUP_DIR/pvc-$TIMESTAMP.yaml"

echo "Backup completed: $TIMESTAMP"
```

## Gotchas and Troubleshooting

### Common Issues

#### 1. UDP Port Limitations with LoadBalancer

**Issue**: Many cloud LoadBalancers limit UDP port ranges

**Solution**: Use NodePort or HostNetwork for RTC traffic

```yaml
apiVersion: v1
kind: Service
metadata:
  name: livekit-rtc
  namespace: livekit
spec:
  type: NodePort
  selector:
    app: livekit-server
  ports:
  - port: 50000
    nodePort: 30000
    protocol: UDP
    name: rtc-50000
  # Map remaining ports
```

Or use HostNetwork:

```yaml
spec:
  template:
    spec:
      hostNetwork: true
      dnsPolicy: ClusterFirstWithHostNet
```

#### 2. Pod Communication Issues

**Diagnosis**:
```bash
# Test pod-to-pod connectivity
kubectl exec -it livekit-server-xxx -n livekit -- ping <other-pod-ip>

# Check network policy
kubectl describe networkpolicy -n livekit

# View service endpoints
kubectl get endpoints -n livekit
```

#### 3. Persistent Storage Issues

**Solution**:
```bash
# Check PVC status
kubectl get pvc -n livekit

# Describe PVC for events
kubectl describe pvc <pvc-name> -n livekit

# Check storage class
kubectl get storageclass
```

#### 4. Image Pull Errors

**Solution**:
```bash
# Check image pull secrets
kubectl get secrets -n livekit

# Create image pull secret
kubectl create secret docker-registry regcred \
  --docker-server=<registry> \
  --docker-username=<username> \
  --docker-password=<password> \
  -n livekit

# Add to deployment
spec:
  template:
    spec:
      imagePullSecrets:
      - name: regcred
```

## Per-Room Server Isolation

For privacy-sensitive deployments, implement per-room server isolation:

```yaml
apiVersion: v1
kind: Service
metadata:
  name: livekit-room-${ROOM_ID}
  namespace: livekit
spec:
  type: ClusterIP
  selector:
    app: livekit-server
    room-id: ${ROOM_ID}
  ports:
  - port: 7880
---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: livekit-room-${ROOM_ID}
  namespace: livekit
spec:
  replicas: 1
  selector:
    matchLabels:
      app: livekit-server
      room-id: ${ROOM_ID}
  template:
    metadata:
      labels:
        app: livekit-server
        room-id: ${ROOM_ID}
    spec:
      containers:
      - name: livekit-server
        image: livekit/livekit-server:latest
        env:
        - name: ROOM_ID
          value: ${ROOM_ID}
```

Automated with Job controller:

```yaml
apiVersion: batch/v1
kind: Job
metadata:
  name: create-room-server-${ROOM_ID}
spec:
  template:
    spec:
      containers:
      - name: room-creator
        image: bitnami/kubectl:latest
        command:
        - /bin/bash
        - -c
        - |
          kubectl apply -f - <<EOF
          # Deployment and Service for room
          EOF
      restartPolicy: OnFailure
```

## Conclusion

Kubernetes deployment is ideal for:
- Large-scale production deployments (1000+ concurrent users)
- Multi-region, globally distributed services
- Auto-scaling requirements
- High availability requirements (99.9%+ uptime)
- Teams with DevOps/Kubernetes expertise
- Organizations with existing Kubernetes infrastructure

Not recommended for:
- Small deployments (< 100 concurrent users)
- Teams without Kubernetes experience
- Budget-constrained projects
- Simple, single-server requirements
- Rapid prototyping or development
