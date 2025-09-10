import * as signalR from '@microsoft/signalr';

export interface EmailStatusUpdate {
  emailJobId: string;
  employeeId: string;
  employeeName: string;
  status: string;
  oldStatus: string;
  timestamp: string;
  eventType: string;
  messageId: string;
}

class SignalRService {
  private connection: signalR.HubConnection | null = null;
  private isConnected = false;
  private reconnectAttempts = 0;
  private maxReconnectAttempts = 5;
  private reconnectDelay = 1000;

  constructor() {
    this.initializeConnection();
  }

  private initializeConnection() {
    const signalRUrl = import.meta.env.VITE_SIGNALR_URL || 'http://localhost:5100/hubs/notifications';
    
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(signalRUrl, {
        skipNegotiation: true,
        transport: signalR.HttpTransportType.WebSockets
      })
      .withAutomaticReconnect([0, 2000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Information)
      .build();

    this.setupEventHandlers();
  }

  private setupEventHandlers() {
    if (!this.connection) return;

    this.connection.onclose((error) => {
      console.log('SignalR connection closed:', error);
      this.isConnected = false;
      this.handleReconnection();
    });

    this.connection.onreconnecting((error) => {
      console.log('SignalR reconnecting:', error);
      this.isConnected = false;
    });

    this.connection.onreconnected((connectionId) => {
      console.log('SignalR reconnected:', connectionId);
      this.isConnected = true;
      this.reconnectAttempts = 0;
    });
  }

  private async handleReconnection() {
    if (this.reconnectAttempts >= this.maxReconnectAttempts) {
      console.log('Max reconnection attempts reached');
      return;
    }

    this.reconnectAttempts++;
    console.log(`Attempting to reconnect (${this.reconnectAttempts}/${this.maxReconnectAttempts})...`);

    setTimeout(async () => {
      try {
        await this.start();
      } catch (error) {
        console.error('Reconnection failed:', error);
        this.handleReconnection();
      }
    }, this.reconnectDelay * this.reconnectAttempts);
  }

  async start(): Promise<void> {
    if (!this.connection) {
      this.initializeConnection();
    }

    if (this.connection.state === signalR.HubConnectionState.Disconnected) {
      try {
        await this.connection.start();
        this.isConnected = true;
        this.reconnectAttempts = 0;
        console.log('SignalR connection started');
      } catch (error) {
        console.error('Error starting SignalR connection:', error);
        throw error;
      }
    }
  }

  async stop(): Promise<void> {
    if (this.connection && this.connection.state !== signalR.HubConnectionState.Disconnected) {
      await this.connection.stop();
      this.isConnected = false;
      console.log('SignalR connection stopped');
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
}

export const signalRService = new SignalRService();
export default signalRService;
