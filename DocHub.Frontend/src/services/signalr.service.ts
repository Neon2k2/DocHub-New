import * as signalR from '@microsoft/signalr';

export interface EmailStatusUpdate {
  emailJobId: string;
  employeeId?: string;
  employeeName?: string;
  employeeEmail?: string;
  status: string;
  oldStatus?: string;
  timestamp: string;
  eventType?: string;
  messageId?: string;
  reason?: string;
  sendGridMessageId?: string;
}

class SignalRService {
  private connection: signalR.HubConnection | null = null;
  private isConnected = false;
  private reconnectAttempts = 0;
  private maxReconnectAttempts = 5;
  private reconnectDelay = 1000;
  private startPromise: Promise<void> | null = null;
  private stopPromise: Promise<void> | null = null;

  constructor() {
    this.initializeConnection();
  }

  // Method to reinitialize connection with new auth token
  async reinitializeConnection(): Promise<void> {
    await this.stop();
    this.initializeConnection();
    await this.start();
  }

  private initializeConnection() {
    const signalRUrl = import.meta.env.VITE_SIGNALR_URL || 'http://localhost:5120/notificationHub';
    
    // Get the auth token from localStorage
    const authToken = localStorage.getItem('authToken');
    
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(signalRUrl, {
        skipNegotiation: true,
        transport: signalR.HttpTransportType.WebSockets,
        accessTokenFactory: () => {
          // Return the current auth token
          return localStorage.getItem('authToken') || '';
        }
      })
      .withAutomaticReconnect([0, 2000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Information)
      .build();

    this.setupEventHandlers();
  }

  private setupEventHandlers() {
    if (!this.connection) return;

    this.connection.onclose((error) => {
      console.log('❌ [SIGNALR_FRONTEND] Connection closed:', error);
      this.isConnected = false;
      this.handleReconnection();
    });

    this.connection.onreconnecting((error) => {
      console.log('🔄 [SIGNALR_FRONTEND] Reconnecting:', error);
      this.isConnected = false;
    });

    this.connection.onreconnected((connectionId) => {
      console.log('✅ [SIGNALR_FRONTEND] Reconnected with connection ID:', connectionId);
      this.isConnected = true;
      this.reconnectAttempts = 0;
    });
  }

  private async handleReconnection() {
    if (this.reconnectAttempts >= this.maxReconnectAttempts) {
      console.log('❌ [SIGNALR_FRONTEND] Max reconnection attempts reached:', this.maxReconnectAttempts);
      return;
    }

    this.reconnectAttempts++;
    console.log(`🔄 [SIGNALR_FRONTEND] Attempting to reconnect (${this.reconnectAttempts}/${this.maxReconnectAttempts})...`);

    setTimeout(async () => {
      try {
        console.log('🚀 [SIGNALR_FRONTEND] Starting reconnection attempt');
        await this.start();
        console.log('✅ [SIGNALR_FRONTEND] Reconnection successful');
      } catch (error) {
        console.error('❌ [SIGNALR_FRONTEND] Reconnection failed:', error);
        this.handleReconnection();
      }
    }, this.reconnectDelay * this.reconnectAttempts);
  }

  async start(): Promise<void> {
    // If already starting, return the existing promise
    if (this.startPromise) {
      return this.startPromise;
    }

    // If currently stopping, wait for it to complete first
    if (this.stopPromise) {
      await this.stopPromise;
    }

    if (!this.connection) {
      this.initializeConnection();
    }

    if (!this.connection) {
      throw new Error('Failed to initialize SignalR connection');
    }

    // Check if already connected
    if (this.connection.state === signalR.HubConnectionState.Connected) {
      this.isConnected = true;
      return;
    }

    // Only start if disconnected
    if (this.connection.state === signalR.HubConnectionState.Disconnected) {
      this.startPromise = this._performStart();
      try {
        await this.startPromise;
      } finally {
        this.startPromise = null;
      }
    }
  }

  private async _performStart(): Promise<void> {
    if (!this.connection) return;
    
    try {
      await this.connection.start();
      this.isConnected = true;
      this.reconnectAttempts = 0;
      console.log('SignalR connection started');
    } catch (error) {
      // Suppress abort errors which are expected during rapid start/stop cycles
      if (error instanceof Error && error.name === 'AbortError') {
        console.log('SignalR start was aborted (likely due to rapid start/stop)');
        this.isConnected = false;
        return; // Don't throw for abort errors
      }
      console.error('Error starting SignalR connection:', error);
      this.isConnected = false;
      throw error;
    }
  }

  async stop(): Promise<void> {
    // If already stopping, return the existing promise
    if (this.stopPromise) {
      return this.stopPromise;
    }

    // If currently starting, wait for it to complete first
    if (this.startPromise) {
      await this.startPromise;
    }

    if (!this.connection) {
      this.isConnected = false;
      return;
    }

    // Check if already disconnected
    if (this.connection.state === signalR.HubConnectionState.Disconnected) {
      this.isConnected = false;
      return;
    }

    // Only stop if not already disconnecting
    if (this.connection.state !== signalR.HubConnectionState.Disconnecting) {
      this.stopPromise = this._performStop();
      try {
        await this.stopPromise;
      } finally {
        this.stopPromise = null;
      }
    }
  }

  private async _performStop(): Promise<void> {
    if (!this.connection) return;
    
    try {
      await this.connection.stop();
      console.log('SignalR connection stopped');
    } catch (error) {
      console.warn('Error stopping SignalR connection:', error);
    } finally {
      this.isConnected = false;
    }
  }

  onEmailStatusUpdated(callback: (update: EmailStatusUpdate) => void): void {
    if (this.connection) {
      this.connection.on('EmailStatusUpdated', callback);
    }
  }

  offEmailStatusUpdated(callback: (update: EmailStatusUpdate) => void): void {
    if (this.connection) {
      this.connection.off('EmailStatusUpdated', callback);
    }
  }

  onNotificationReceived(callback: (notification: any) => void): void {
    if (this.connection) {
      this.connection.on('NotificationReceived', callback);
    }
  }

  offNotificationReceived(callback: (notification: any) => void): void {
    if (this.connection) {
      this.connection.off('NotificationReceived', callback);
    }
  }

  getConnectionState(): signalR.HubConnectionState {
    return this.connection?.state || signalR.HubConnectionState.Disconnected;
  }

  isConnectionActive(): boolean {
    return this.isConnected && this.connection?.state === signalR.HubConnectionState.Connected;
  }

  // Method to join a user group
  async joinUserGroup(userId: string): Promise<void> {
    if (this.connection && this.isConnected) {
      try {
        await this.connection.invoke('JoinGroup', `user_${userId}`);
        console.log(`✅ [SIGNALR] Joined user group: user_${userId}`);
      } catch (error) {
        console.error(`❌ [SIGNALR] Failed to join user group: user_${userId}`, error);
      }
    }
  }

  // Method to leave a user group
  async leaveUserGroup(userId: string): Promise<void> {
    if (this.connection && this.isConnected) {
      try {
        await this.connection.invoke('LeaveGroup', `user_${userId}`);
        console.log(`✅ [SIGNALR] Left user group: user_${userId}`);
      } catch (error) {
        console.error(`❌ [SIGNALR] Failed to leave user group: user_${userId}`, error);
      }
    }
  }
}

export const signalRService = new SignalRService();
export default signalRService;
