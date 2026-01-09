# LiveKit Load Balancing and Auto-Scaling Architecture Guide

## Overview

This guide covers advanced deployment patterns for LiveKit with load balancing, auto-scaling, and dynamic resource management. These patterns enable efficient resource utilization while maintaining high availability and performance.

## Table of Contents

1. [Load Balancing Strategies](#load-balancing-strategies)
2. [Auto-Scaling Patterns](#auto-scaling-patterns)
3. [Per-Room Server Isolation](#per-room-server-isolation)
4. [Dynamic Server Provisioning](#dynamic-server-provisioning)
5. [Traffic Routing and Selection](#traffic-routing-and-selection)
6. [Implementation Examples](#implementation-examples)
7. [Monitoring and Optimization](#monitoring-and-optimization)

---

## Load Balancing Strategies

### 1. Layer 4 (TCP/UDP) Load Balancing

**Best for**: WebRTC media traffic

**Characteristics**:
- Direct TCP/UDP forwarding
- Lowest latency
- Session affinity required
- Works at transport layer

#### HAProxy Configuration

```haproxy
# /etc/haproxy/haproxy.cfg
global
    log /dev/log local0
    maxconn 50000
    tune.ssl.default-dh-param 2048

defaults
    log     global
    mode    tcp
    option  tcplog
    option  dontlognull
    timeout connect 5000
    timeout client  50000
    timeout server  50000

# Frontend for HTTP/WebSocket
frontend livekit_http
    bind *:7880
    mode tcp
    default_backend livekit_servers_http

# Frontend for HTTPS
frontend livekit_https
    bind *:7881 ssl crt /etc/ssl/certs/livekit.pem
    mode tcp
    default_backend livekit_servers_http

# Backend servers
backend livekit_servers_http
    mode tcp
    balance leastconn
    option tcp-check
    
    # Sticky sessions based on source IP
    stick-table type ip size 200k expire 30m
    stick on src
    
    # Health check
    tcp-check connect port 7880
    tcp-check send HEAD\ /health\ HTTP/1.1\r\nHost:\ livekit\r\n\r\n
    tcp-check expect string 200
    
    # Server definitions
    server livekit1 10.0.1.10:7880 check inter 5s fall 3 rise 2
    server livekit2 10.0.1.11:7880 check inter 5s fall 3 rise 2
    server livekit3 10.0.1.12:7880 check inter 5s fall 3 rise 2

# Stats interface
listen stats
    bind *:8080
    mode http
    stats enable
    stats uri /stats
    stats refresh 10s
    stats admin if TRUE
```

#### NGINX Stream Configuration

```nginx
# /etc/nginx/nginx.conf
stream {
    upstream livekit_http {
        least_conn;
        
        # Enable session persistence
        hash $remote_addr consistent;
        
        server 10.0.1.10:7880 max_fails=3 fail_timeout=30s;
        server 10.0.1.11:7880 max_fails=3 fail_timeout=30s;
        server 10.0.1.12:7880 max_fails=3 fail_timeout=30s;
    }
    
    upstream livekit_udp {
        hash $remote_addr consistent;
        
        server 10.0.1.10:50000;
        server 10.0.1.11:50000;
        server 10.0.1.12:50000;
    }
    
    # HTTP/WebSocket
    server {
        listen 7880;
        proxy_pass livekit_http;
        proxy_timeout 30m;
        proxy_connect_timeout 10s;
    }
    
    # UDP media traffic
    server {
        listen 50000 udp;
        proxy_pass livekit_udp;
        proxy_timeout 5s;
        proxy_responses 1;
    }
}
```

### 2. Application Layer (Layer 7) Load Balancing

**Best for**: API and WebSocket traffic

**Characteristics**:
- Content-aware routing
- SSL termination
- Path-based routing
- Header-based routing

#### NGINX Layer 7 Configuration

```nginx
# /etc/nginx/conf.d/livekit.conf
upstream livekit_backend {
    # Load balancing method
    least_conn;
    
    # Keepalive connections
    keepalive 100;
    
    # Servers
    server 10.0.1.10:7880 weight=1 max_fails=3 fail_timeout=30s;
    server 10.0.1.11:7880 weight=1 max_fails=3 fail_timeout=30s;
    server 10.0.1.12:7880 weight=1 max_fails=3 fail_timeout=30s;
}

# Health check endpoint
server {
    listen 8080;
    
    location /health {
        access_log off;
        return 200 "healthy\n";
        add_header Content-Type text/plain;
    }
}

server {
    listen 80;
    server_name livekit.example.com;
    
    # Redirect to HTTPS
    return 301 https://$host$request_uri;
}

server {
    listen 443 ssl http2;
    server_name livekit.example.com;
    
    # SSL configuration
    ssl_certificate /etc/ssl/certs/livekit.crt;
    ssl_certificate_key /etc/ssl/private/livekit.key;
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers HIGH:!aNULL:!MD5;
    ssl_prefer_server_ciphers on;
    ssl_session_cache shared:SSL:10m;
    ssl_session_timeout 10m;
    
    # WebSocket configuration
    location / {
        proxy_pass http://livekit_backend;
        
        # WebSocket headers
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        
        # Standard proxy headers
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        
        # Timeouts for WebSocket
        proxy_connect_timeout 7d;
        proxy_send_timeout 7d;
        proxy_read_timeout 7d;
        
        # Buffer settings
        proxy_buffering off;
        proxy_request_buffering off;
    }
    
    # Health check endpoint
    location /health {
        proxy_pass http://livekit_backend/health;
        access_log off;
    }
    
    # Metrics endpoint
    location /metrics {
        proxy_pass http://livekit_backend/metrics;
        
        # Restrict access
        allow 10.0.0.0/8;
        deny all;
    }
}
```

### 3. DNS-Based Load Balancing

**Best for**: Geographic distribution

```bash
# Using AWS Route 53
aws route53 change-resource-record-sets \
  --hosted-zone-id Z1234567890ABC \
  --change-batch file://dns-records.json
```

`dns-records.json`:
```json
{
  "Changes": [
    {
      "Action": "UPSERT",
      "ResourceRecordSet": {
        "Name": "livekit.example.com",
        "Type": "A",
        "SetIdentifier": "US-West",
        "Region": "us-west-2",
        "TTL": 60,
        "ResourceRecords": [
          {"Value": "54.1.2.3"}
        ],
        "HealthCheckId": "abc123"
      }
    },
    {
      "Action": "UPSERT",
      "ResourceRecordSet": {
        "Name": "livekit.example.com",
        "Type": "A",
        "SetIdentifier": "EU-Central",
        "Region": "eu-central-1",
        "TTL": 60,
        "ResourceRecords": [
          {"Value": "54.4.5.6"}
        ],
        "HealthCheckId": "def456"
      }
    }
  ]
}
```

### 4. Anycast Load Balancing

**Best for**: Global deployments with lowest latency

**Architecture**:
```
Client Request → Nearest PoP (Anycast IP)
    ↓
Regional Load Balancer
    ↓
LiveKit Server Pool
```

---

## Auto-Scaling Patterns

### 1. Metric-Based Auto-Scaling

#### CPU and Memory Based Scaling

**Kubernetes HPA with Multiple Metrics**:

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
  maxReplicas: 50
  metrics:
  # CPU utilization
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 70
  
  # Memory utilization
  - type: Resource
    resource:
      name: memory
      target:
        type: Utilization
        averageUtilization: 80
  
  # Custom metric: active rooms
  - type: Pods
    pods:
      metric:
        name: livekit_active_rooms
      target:
        type: AverageValue
        averageValue: "20"
  
  # Custom metric: participants
  - type: Pods
    pods:
      metric:
        name: livekit_active_participants
      target:
        type: AverageValue
        averageValue: "100"
  
  behavior:
    scaleDown:
      stabilizationWindowSeconds: 300
      policies:
      - type: Percent
        value: 25
        periodSeconds: 60
      - type: Pods
        value: 2
        periodSeconds: 60
      selectPolicy: Min
    
    scaleUp:
      stabilizationWindowSeconds: 0
      policies:
      - type: Percent
        value: 50
        periodSeconds: 30
      - type: Pods
        value: 5
        periodSeconds: 30
      selectPolicy: Max
```

#### Custom Metrics Adapter

```yaml
# custom-metrics-config.yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: adapter-config
  namespace: custom-metrics
data:
  config.yaml: |
    rules:
    - seriesQuery: 'livekit_room_total'
      resources:
        template: <<.Resource>>
      name:
        matches: "^livekit_room_total"
        as: "livekit_active_rooms"
      metricsQuery: 'sum(livekit_room_total{status="active"}) by (<<.GroupBy>>)'
    
    - seriesQuery: 'livekit_participant_total'
      resources:
        template: <<.Resource>>
      name:
        matches: "^livekit_participant_total"
        as: "livekit_active_participants"
      metricsQuery: 'sum(livekit_participant_total) by (<<.GroupBy>>)'
```

### 2. Queue-Based Auto-Scaling

**Pattern**: Scale based on pending connections

```python
# scale_controller.py
import boto3
import requests
from datetime import datetime, timedelta

class LiveKitScaler:
    def __init__(self):
        self.ecs_client = boto3.client('ecs')
        self.cloudwatch = boto3.client('cloudwatch')
        
    def get_active_rooms(self):
        """Get active room count from all LiveKit instances"""
        # Query load balancer for all backend IPs
        backends = self.get_backend_ips()
        total_rooms = 0
        
        for backend in backends:
            try:
                resp = requests.get(f'http://{backend}:7880/rooms', timeout=5)
                total_rooms += len(resp.json().get('rooms', []))
            except:
                continue
                
        return total_rooms
    
    def get_pending_connections(self):
        """Get number of pending WebSocket connections"""
        response = self.cloudwatch.get_metric_statistics(
            Namespace='AWS/ApplicationELB',
            MetricName='TargetResponseTime',
            Dimensions=[
                {'Name': 'LoadBalancer', 'Value': 'app/livekit-alb/...'}
            ],
            StartTime=datetime.now() - timedelta(minutes=5),
            EndTime=datetime.now(),
            Period=60,
            Statistics=['Average']
        )
        return response['Datapoints']
    
    def calculate_required_capacity(self):
        """Calculate how many instances needed"""
        active_rooms = self.get_active_rooms()
        
        # Assume 20 rooms per instance optimal
        ROOMS_PER_INSTANCE = 20
        MIN_INSTANCES = 3
        MAX_INSTANCES = 50
        
        required = max(MIN_INSTANCES, (active_rooms // ROOMS_PER_INSTANCE) + 1)
        return min(required, MAX_INSTANCES)
    
    def scale(self):
        """Execute scaling decision"""
        target_count = self.calculate_required_capacity()
        current_count = self.get_current_task_count()
        
        if target_count != current_count:
            print(f"Scaling from {current_count} to {target_count}")
            self.ecs_client.update_service(
                cluster='livekit-cluster',
                service='livekit-service',
                desiredCount=target_count
            )
    
    def get_current_task_count(self):
        """Get current ECS task count"""
        response = self.ecs_client.describe_services(
            cluster='livekit-cluster',
            services=['livekit-service']
        )
        return response['services'][0]['desiredCount']

if __name__ == '__main__':
    scaler = LiveKitScaler()
    scaler.scale()
```

### 3. Predictive Auto-Scaling

**Pattern**: Scale based on historical patterns

```python
# predictive_scaler.py
import pandas as pd
from sklearn.ensemble import RandomForestRegressor
import boto3
from datetime import datetime, timedelta

class PredictiveScaler:
    def __init__(self):
        self.model = RandomForestRegressor(n_estimators=100)
        self.ecs_client = boto3.client('ecs')
        
    def collect_historical_data(self, days=30):
        """Collect historical usage data"""
        # Query CloudWatch or database for historical metrics
        data = []
        for day in range(days):
            date = datetime.now() - timedelta(days=day)
            metrics = self.get_metrics_for_date(date)
            data.append({
                'hour': date.hour,
                'day_of_week': date.weekday(),
                'rooms': metrics['rooms'],
                'participants': metrics['participants']
            })
        return pd.DataFrame(data)
    
    def train_model(self):
        """Train prediction model"""
        df = self.collect_historical_data()
        X = df[['hour', 'day_of_week']]
        y = df['rooms']
        self.model.fit(X, y)
    
    def predict_next_hour(self):
        """Predict load for next hour"""
        now = datetime.now() + timedelta(hours=1)
        prediction = self.model.predict([[now.hour, now.weekday()]])
        return int(prediction[0])
    
    def scale_proactively(self):
        """Scale based on prediction"""
        predicted_rooms = self.predict_next_hour()
        required_instances = max(3, predicted_rooms // 20 + 1)
        
        print(f"Predicted {predicted_rooms} rooms, scaling to {required_instances} instances")
        
        self.ecs_client.update_service(
            cluster='livekit-cluster',
            service='livekit-service',
            desiredCount=required_instances
        )
```

### 4. Event-Driven Auto-Scaling

**Pattern**: Scale on specific events (room created/destroyed)

```javascript
// event_scaler.js
const AWS = require('aws-sdk');
const ecs = new AWS.ECS();

class EventDrivenScaler {
    constructor() {
        this.roomCount = 0;
        this.lastScaleTime = Date.now();
        this.scaleThreshold = 30000; // 30 seconds between scales
    }
    
    async handleRoomCreated(event) {
        this.roomCount++;
        console.log(`Room created. Total: ${this.roomCount}`);
        
        if (this.shouldScale()) {
            await this.scaleUp();
        }
    }
    
    async handleRoomDestroyed(event) {
        this.roomCount = Math.max(0, this.roomCount - 1);
        console.log(`Room destroyed. Total: ${this.roomCount}`);
        
        if (this.shouldScale()) {
            await this.scaleDown();
        }
    }
    
    shouldScale() {
        const timeSinceLastScale = Date.now() - this.lastScaleTime;
        return timeSinceLastScale > this.scaleThreshold;
    }
    
    async scaleUp() {
        const currentTasks = await this.getCurrentTaskCount();
        const newCount = Math.min(currentTasks + 2, 50);
        
        console.log(`Scaling up from ${currentTasks} to ${newCount}`);
        
        await ecs.updateService({
            cluster: 'livekit-cluster',
            service: 'livekit-service',
            desiredCount: newCount
        }).promise();
        
        this.lastScaleTime = Date.now();
    }
    
    async scaleDown() {
        const currentTasks = await this.getCurrentTaskCount();
        
        // Only scale down if room density is low
        const roomsPerTask = this.roomCount / currentTasks;
        
        if (roomsPerTask < 10 && currentTasks > 3) {
            const newCount = Math.max(currentTasks - 1, 3);
            console.log(`Scaling down from ${currentTasks} to ${newCount}`);
            
            await ecs.updateService({
                cluster: 'livekit-cluster',
                service: 'livekit-service',
                desiredCount: newCount
            }).promise();
            
            this.lastScaleTime = Date.now();
        }
    }
    
    async getCurrentTaskCount() {
        const response = await ecs.describeServices({
            cluster: 'livekit-cluster',
            services: ['livekit-service']
        }).promise();
        
        return response.services[0].desiredCount;
    }
}

module.exports = EventDrivenScaler;
```

---

## Per-Room Server Isolation

### Architecture Pattern

**Isolation Strategy**: Deploy separate server instance per room for maximum privacy

```yaml
# room-isolation-pattern.yaml
apiVersion: v1
kind: Service
metadata:
  name: livekit-room-{{ROOM_ID}}
  namespace: livekit-rooms
  labels:
    app: livekit-room-server
    room-id: {{ROOM_ID}}
spec:
  type: LoadBalancer
  selector:
    app: livekit-room-server
    room-id: {{ROOM_ID}}
  ports:
  - name: http
    port: 7880
    targetPort: 7880
  - name: rtc-udp
    port: 50000
    targetPort: 50000
    protocol: UDP
---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: livekit-room-{{ROOM_ID}}
  namespace: livekit-rooms
spec:
  replicas: 1
  selector:
    matchLabels:
      app: livekit-room-server
      room-id: {{ROOM_ID}}
  template:
    metadata:
      labels:
        app: livekit-room-server
        room-id: {{ROOM_ID}}
      annotations:
        room-created-at: {{TIMESTAMP}}
    spec:
      containers:
      - name: livekit-server
        image: livekit/livekit-server:v1.5.3
        env:
        - name: ROOM_ID
          value: {{ROOM_ID}}
        - name: ROOM_MODE
          value: "isolated"
        resources:
          requests:
            cpu: 1000m
            memory: 2Gi
          limits:
            cpu: 2000m
            memory: 4Gi
      # Auto-cleanup after room ends
      ttlSecondsAfterFinished: 300
```

### Room Lifecycle Manager

```python
# room_lifecycle_manager.py
from kubernetes import client, config
import hashlib
import time

class RoomLifecycleManager:
    def __init__(self):
        config.load_kube_config()
        self.apps_v1 = client.AppsV1Api()
        self.core_v1 = client.CoreV1Api()
        self.namespace = 'livekit-rooms'
        
    def create_room_server(self, room_id, room_config):
        """Create dedicated server for room"""
        deployment_name = f"livekit-room-{room_id}"
        
        # Check if already exists
        try:
            self.apps_v1.read_namespaced_deployment(
                deployment_name, self.namespace
            )
            print(f"Room server {room_id} already exists")
            return
        except client.exceptions.ApiException as e:
            if e.status != 404:
                raise
        
        # Create deployment
        deployment = self._build_deployment_manifest(room_id, room_config)
        self.apps_v1.create_namespaced_deployment(
            self.namespace, deployment
        )
        
        # Create service
        service = self._build_service_manifest(room_id)
        self.core_v1.create_namespaced_service(
            self.namespace, service
        )
        
        print(f"Created room server for {room_id}")
        
        # Wait for pod to be ready
        self._wait_for_pod_ready(deployment_name)
        
        return self._get_service_endpoint(room_id)
    
    def destroy_room_server(self, room_id):
        """Destroy dedicated room server"""
        deployment_name = f"livekit-room-{room_id}"
        service_name = f"livekit-room-{room_id}"
        
        try:
            # Delete deployment
            self.apps_v1.delete_namespaced_deployment(
                deployment_name,
                self.namespace,
                body=client.V1DeleteOptions(
                    propagation_policy='Foreground'
                )
            )
            
            # Delete service
            self.core_v1.delete_namespaced_service(
                service_name,
                self.namespace
            )
            
            print(f"Destroyed room server for {room_id}")
        except client.exceptions.ApiException as e:
            if e.status == 404:
                print(f"Room server {room_id} not found")
            else:
                raise
    
    def _build_deployment_manifest(self, room_id, config):
        """Build Kubernetes deployment manifest"""
        return client.V1Deployment(
            metadata=client.V1ObjectMeta(
                name=f"livekit-room-{room_id}",
                labels={
                    'app': 'livekit-room-server',
                    'room-id': room_id
                }
            ),
            spec=client.V1DeploymentSpec(
                replicas=1,
                selector=client.V1LabelSelector(
                    match_labels={
                        'app': 'livekit-room-server',
                        'room-id': room_id
                    }
                ),
                template=client.V1PodTemplateSpec(
                    metadata=client.V1ObjectMeta(
                        labels={
                            'app': 'livekit-room-server',
                            'room-id': room_id
                        }
                    ),
                    spec=client.V1PodSpec(
                        containers=[
                            client.V1Container(
                                name='livekit-server',
                                image='livekit/livekit-server:v1.5.3',
                                ports=[
                                    client.V1ContainerPort(
                                        container_port=7880,
                                        protocol='TCP'
                                    ),
                                    client.V1ContainerPort(
                                        container_port=50000,
                                        protocol='UDP'
                                    )
                                ],
                                env=[
                                    client.V1EnvVar(
                                        name='ROOM_ID',
                                        value=room_id
                                    ),
                                    client.V1EnvVar(
                                        name='MAX_PARTICIPANTS',
                                        value=str(config.get('max_participants', 50))
                                    )
                                ],
                                resources=client.V1ResourceRequirements(
                                    requests={
                                        'cpu': '1000m',
                                        'memory': '2Gi'
                                    },
                                    limits={
                                        'cpu': '2000m',
                                        'memory': '4Gi'
                                    }
                                )
                            )
                        ]
                    )
                )
            )
        )
    
    def _build_service_manifest(self, room_id):
        """Build Kubernetes service manifest"""
        return client.V1Service(
            metadata=client.V1ObjectMeta(
                name=f"livekit-room-{room_id}",
                labels={
                    'app': 'livekit-room-server',
                    'room-id': room_id
                }
            ),
            spec=client.V1ServiceSpec(
                type='LoadBalancer',
                selector={
                    'app': 'livekit-room-server',
                    'room-id': room_id
                },
                ports=[
                    client.V1ServicePort(
                        name='http',
                        port=7880,
                        target_port=7880,
                        protocol='TCP'
                    ),
                    client.V1ServicePort(
                        name='rtc-udp',
                        port=50000,
                        target_port=50000,
                        protocol='UDP'
                    )
                ]
            )
        )
    
    def _wait_for_pod_ready(self, deployment_name, timeout=120):
        """Wait for pod to be ready"""
        start_time = time.time()
        
        while time.time() - start_time < timeout:
            deployment = self.apps_v1.read_namespaced_deployment(
                deployment_name, self.namespace
            )
            
            if (deployment.status.ready_replicas and 
                deployment.status.ready_replicas > 0):
                return True
            
            time.sleep(2)
        
        raise TimeoutError(f"Pod {deployment_name} not ready after {timeout}s")
    
    def _get_service_endpoint(self, room_id):
        """Get service external endpoint"""
        service_name = f"livekit-room-{room_id}"
        
        # Wait for LoadBalancer IP
        for _ in range(60):
            service = self.core_v1.read_namespaced_service(
                service_name, self.namespace
            )
            
            if service.status.load_balancer.ingress:
                ingress = service.status.load_balancer.ingress[0]
                if ingress.ip:
                    return f"http://{ingress.ip}:7880"
                elif ingress.hostname:
                    return f"http://{ingress.hostname}:7880"
            
            time.sleep(2)
        
        raise TimeoutError("LoadBalancer IP not assigned")
    
    def cleanup_idle_rooms(self, idle_threshold=3600):
        """Clean up rooms with no activity"""
        deployments = self.apps_v1.list_namespaced_deployment(
            self.namespace,
            label_selector='app=livekit-room-server'
        )
        
        for deployment in deployments.items:
            room_id = deployment.metadata.labels.get('room-id')
            
            # Check if room is idle
            if self._is_room_idle(room_id, idle_threshold):
                print(f"Cleaning up idle room: {room_id}")
                self.destroy_room_server(room_id)
    
    def _is_room_idle(self, room_id, threshold):
        """Check if room has been idle"""
        # Implementation depends on your monitoring system
        # Query Prometheus, CloudWatch, or LiveKit API
        return False  # Placeholder
```

---

## Dynamic Server Provisioning

### On-Demand Server Spawning

```bash
#!/bin/bash
# spawn_server.sh - Spawn server on demand

ROOM_ID=$1
SERVER_TYPE=${2:-"standard"}  # standard, high-capacity, low-latency

case $SERVER_TYPE in
    "standard")
        CPU="2000m"
        MEMORY="4Gi"
        ;;
    "high-capacity")
        CPU="4000m"
        MEMORY="8Gi"
        ;;
    "low-latency")
        CPU="4000m"
        MEMORY="4Gi"
        # Deploy to closest region
        ;;
esac

# Create server
kubectl create -f - <<EOF
apiVersion: apps/v1
kind: Deployment
metadata:
  name: livekit-${ROOM_ID}
  namespace: livekit
spec:
  replicas: 1
  selector:
    matchLabels:
      room: ${ROOM_ID}
  template:
    metadata:
      labels:
        room: ${ROOM_ID}
    spec:
      containers:
      - name: livekit
        image: livekit/livekit-server:latest
        resources:
          requests:
            cpu: ${CPU}
            memory: ${MEMORY}
          limits:
            cpu: ${CPU}
            memory: ${MEMORY}
EOF

# Wait for ready
kubectl wait --for=condition=available --timeout=60s \
  deployment/livekit-${ROOM_ID} -n livekit

echo "Server spawned for room ${ROOM_ID}"
```

### Serverless Function for Room Orchestration

```javascript
// lambda/room_orchestrator.js
const AWS = require('aws-sdk');
const ecs = new AWS.ECS();

exports.handler = async (event) => {
    const { roomId, participants, recordingEnabled } = event;
    
    // Determine resource requirements
    const resources = calculateResources(participants);
    
    // Choose deployment strategy
    if (participants > 100 || recordingEnabled) {
        // Dedicated server
        await spawnDedicatedServer(roomId, resources);
    } else {
        // Use shared pool
        await assignToSharedPool(roomId);
    }
    
    return {
        statusCode: 200,
        body: JSON.stringify({
            roomId: roomId,
            serverUrl: await getServerUrl(roomId)
        })
    };
};

function calculateResources(participants) {
    // Base resources
    let cpu = 1024; // 1 vCPU
    let memory = 2048; // 2GB
    
    // Scale with participants
    if (participants > 50) {
        cpu = 2048;
        memory = 4096;
    }
    if (participants > 100) {
        cpu = 4096;
        memory = 8192;
    }
    
    return { cpu, memory };
}

async function spawnDedicatedServer(roomId, resources) {
    const taskDef = {
        family: `livekit-room-${roomId}`,
        cpu: resources.cpu.toString(),
        memory: resources.memory.toString(),
        networkMode: 'awsvpc',
        requiresCompatibilities: ['FARGATE'],
        containerDefinitions: [
            {
                name: 'livekit',
                image: 'livekit/livekit-server:latest',
                portMappings: [
                    { containerPort: 7880, protocol: 'tcp' }
                ],
                environment: [
                    { name: 'ROOM_ID', value: roomId }
                ]
            }
        ]
    };
    
    // Register task definition
    await ecs.registerTaskDefinition(taskDef).promise();
    
    // Run task
    await ecs.runTask({
        cluster: 'livekit-cluster',
        taskDefinition: `livekit-room-${roomId}`,
        launchType: 'FARGATE',
        count: 1,
        networkConfiguration: {
            awsvpcConfiguration: {
                subnets: ['subnet-xxx'],
                securityGroups: ['sg-xxx'],
                assignPublicIp: 'ENABLED'
            }
        }
    }).promise();
}
```

---

## Traffic Routing and Selection

### Intelligent Room Placement

```python
# room_placement_algorithm.py
from typing import List, Dict
import requests

class ServerSelector:
    def __init__(self):
        self.servers = []
        self.load_threshold = 0.8
    
    def select_server(self, room_requirements: Dict) -> str:
        """Select best server for new room"""
        # Fetch current server states
        self.servers = self._fetch_server_states()
        
        # Filter viable servers
        viable = [s for s in self.servers if self._is_viable(s, room_requirements)]
        
        if not viable:
            # Spawn new server
            return self._spawn_new_server(room_requirements)
        
        # Score servers
        scored = [(s, self._score_server(s, room_requirements)) for s in viable]
        scored.sort(key=lambda x: x[1], reverse=True)
        
        return scored[0][0]['url']
    
    def _fetch_server_states(self) -> List[Dict]:
        """Fetch state of all available servers"""
        servers = []
        # Query service discovery or load balancer
        endpoints = self._get_server_endpoints()
        
        for endpoint in endpoints:
            try:
                resp = requests.get(f"{endpoint}/stats", timeout=2)
                stats = resp.json()
                servers.append({
                    'url': endpoint,
                    'rooms': stats.get('rooms', 0),
                    'participants': stats.get('participants', 0),
                    'cpu_usage': stats.get('cpu_usage', 0),
                    'memory_usage': stats.get('memory_usage', 0),
                    'region': stats.get('region', 'unknown'),
                    'latency': self._measure_latency(endpoint)
                })
            except:
                continue
        
        return servers
    
    def _is_viable(self, server: Dict, requirements: Dict) -> bool:
        """Check if server can handle room"""
        # CPU check
        if server['cpu_usage'] > self.load_threshold:
            return False
        
        # Memory check
        if server['memory_usage'] > self.load_threshold:
            return False
        
        # Capacity check
        max_participants = requirements.get('max_participants', 50)
        if server['participants'] + max_participants > 200:
            return False
        
        # Region preference
        preferred_region = requirements.get('region')
        if preferred_region and server['region'] != preferred_region:
            return False
        
        return True
    
    def _score_server(self, server: Dict, requirements: Dict) -> float:
        """Score server based on multiple factors"""
        score = 100.0
        
        # Prefer servers with lower load
        score -= server['cpu_usage'] * 50
        score -= server['memory_usage'] * 30
        
        # Prefer servers with fewer rooms (better isolation)
        score -= server['rooms'] * 2
        
        # Latency matters
        score -= server['latency'] / 10
        
        # Regional preference bonus
        if server['region'] == requirements.get('region'):
            score += 20
        
        return score
    
    def _spawn_new_server(self, requirements: Dict) -> str:
        """Spawn new server for room"""
        # Trigger auto-scaling or create new instance
        # Implementation depends on infrastructure
        pass
    
    def _get_server_endpoints(self) -> List[str]:
        """Get list of server endpoints"""
        # Query Kubernetes, Consul, or cloud service discovery
        pass
    
    def _measure_latency(self, endpoint: str) -> float:
        """Measure latency to endpoint"""
        import time
        start = time.time()
        try:
            requests.get(f"{endpoint}/health", timeout=2)
            return (time.time() - start) * 1000  # ms
        except:
            return 9999.0  # High penalty for unreachable
```

---

## Monitoring and Optimization

### Comprehensive Monitoring Stack

```yaml
# monitoring-stack.yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: prometheus-config
  namespace: monitoring
data:
  prometheus.yml: |
    global:
      scrape_interval: 15s
    
    scrape_configs:
    - job_name: 'livekit'
      kubernetes_sd_configs:
      - role: pod
        namespaces:
          names:
          - livekit
      relabel_configs:
      - source_labels: [__meta_kubernetes_pod_label_app]
        action: keep
        regex: livekit-server
      
      # Custom metrics
      metric_relabel_configs:
      - source_labels: [__name__]
        regex: 'livekit_(room|participant|track).*'
        action: keep
    
    # Alerting rules
    rule_files:
    - /etc/prometheus/alerts.yml
```

### Performance Optimization Script

```bash
#!/bin/bash
# optimize_deployment.sh

echo "=== LiveKit Deployment Optimization ==="

# 1. Analyze current load distribution
echo "Analyzing load distribution..."
kubectl top pods -n livekit -l app=livekit-server

# 2. Check for imbalanced pods
echo "Checking for load imbalance..."
kubectl get pods -n livekit -o json | jq -r '
  .items[] |
  select(.metadata.labels.app=="livekit-server") |
  "\(.metadata.name): CPU=\(.status.containerStatuses[0].resources.requests.cpu) MEM=\(.status.containerStatuses[0].resources.requests.memory)"
'

# 3. Recommend scaling adjustments
CURRENT_REPLICAS=$(kubectl get deployment livekit-server -n livekit -o jsonpath='{.spec.replicas}')
AVG_CPU=$(kubectl top pods -n livekit -l app=livekit-server --no-headers | awk '{sum+=$2} END {print sum/NR}')

if (( $(echo "$AVG_CPU > 70" | bc -l) )); then
    RECOMMENDED=$((CURRENT_REPLICAS + 2))
    echo "High CPU detected. Recommend scaling to $RECOMMENDED replicas"
elif (( $(echo "$AVG_CPU < 30" | bc -l) )) && [ $CURRENT_REPLICAS -gt 3 ]; then
    RECOMMENDED=$((CURRENT_REPLICAS - 1))
    echo "Low CPU detected. Recommend scaling down to $RECOMMENDED replicas"
else
    echo "Current scaling is optimal"
fi

# 4. Check for stuck pods
echo "Checking for problematic pods..."
kubectl get pods -n livekit -l app=livekit-server --field-selector=status.phase!=Running

# 5. Network latency analysis
echo "Analyzing network latency..."
kubectl exec -n livekit $(kubectl get pod -n livekit -l app=livekit-server -o jsonpath='{.items[0].metadata.name}') -- \
    sh -c 'apk add --no-cache curl && curl -w "@-" -o /dev/null -s http://livekit-service:7880/health <<< "time_total: %{time_total}\n"'
```

## Conclusion

Effective load balancing and auto-scaling require:
- **Multi-layered approach**: Combine L4 and L7 load balancing
- **Intelligent metrics**: Use application-specific metrics
- **Predictive scaling**: Anticipate load changes
- **Room isolation**: When privacy/performance demands
- **Continuous monitoring**: Real-time observability
- **Cost optimization**: Balance performance and cost
- **Graceful degradation**: Handle failures elegantly

Choose patterns based on:
- **Traffic patterns**: Predictable vs unpredictable
- **Privacy requirements**: Shared vs isolated
- **Scale requirements**: Small vs large deployments
- **Budget constraints**: Cost vs performance
- **Operational complexity**: Team capabilities
