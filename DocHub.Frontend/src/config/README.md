# Environment Configuration

## Setup Instructions

1. Create a `.env.local` file in the root directory with the following variables:

```env
# API Configuration
VITE_API_BASE_URL=http://localhost:5001/api/v1
VITE_SIGNALR_URL=http://localhost:5001/hubs/notifications

# Application Configuration
VITE_APP_NAME=DocHub
VITE_APP_VERSION=1.0.0

# Feature Flags
VITE_ENABLE_NOTIFICATIONS=true
VITE_ENABLE_REAL_TIME_UPDATES=true
VITE_ENABLE_OFFLINE_MODE=false

# Development Settings
VITE_DEBUG_MODE=true
VITE_LOG_LEVEL=info
```

## Environment Variables

| Variable | Description | Default | Required |
|----------|-------------|---------|----------|
| `VITE_API_BASE_URL` | Backend API base URL | `http://localhost:5001/api/v1` | Yes |
| `VITE_SIGNALR_URL` | SignalR hub URL | `http://localhost:5001/hubs/notifications` | Yes |
| `VITE_APP_NAME` | Application name | `DocHub` | No |
| `VITE_APP_VERSION` | Application version | `1.0.0` | No |
| `VITE_ENABLE_NOTIFICATIONS` | Enable notifications | `true` | No |
| `VITE_ENABLE_REAL_TIME_UPDATES` | Enable real-time updates | `true` | No |
| `VITE_ENABLE_OFFLINE_MODE` | Enable offline mode | `false` | No |
| `VITE_DEBUG_MODE` | Enable debug mode | `true` | No |
| `VITE_LOG_LEVEL` | Logging level | `info` | No |

## Production Configuration

For production deployment, set these environment variables:

```env
VITE_API_BASE_URL=https://api.yourdomain.com/api/v1
VITE_SIGNALR_URL=https://api.yourdomain.com/hubs/notifications
VITE_APP_NAME=DocHub
VITE_APP_VERSION=1.0.0
VITE_DEBUG_MODE=false
VITE_LOG_LEVEL=error
```
