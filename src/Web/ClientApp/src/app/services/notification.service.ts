import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private connection: signalR.HubConnection;
  notification$ = new Subject<string>();

  constructor() {
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl('/chat')
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Information)
      .build();

    this.connection.on('ReceiveNotification', (message: string) => {
      console.log('[SignalR] ReceiveNotification:', message);
      this.notification$.next(message);
    });
  }

  async connect(): Promise<void> {
    if (this.connection.state === signalR.HubConnectionState.Disconnected) {
      await this.connection.start();
    }
  }

  async disconnect(): Promise<void> {
    await this.connection.stop();
  }
}
