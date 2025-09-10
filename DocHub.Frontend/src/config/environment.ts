// Environment configuration for DocHub Frontend
declare const __API_BASE_URL__: string;
declare const __SIGNALR_URL__: string;
declare const __APP_NAME__: string;
declare const __APP_VERSION__: string;

export const config = {
  api: {
    baseUrl: __API_BASE_URL__,
    timeout: 30000, // 30 seconds
    retryAttempts: 3,
  },
  signalr: {
    url: __SIGNALR_URL__,
    reconnectInterval: 5000, // 5 seconds
    maxReconnectAttempts: 5,
  },
  app: {
    name: __APP_NAME__,
    version: __APP_VERSION__,
  },
  features: {
    enableNotifications: true,
    enableRealTimeUpdates: true,
    enableOfflineMode: false,
  },
} as const;

export type Config = typeof config;
