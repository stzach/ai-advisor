
import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';


@Injectable({ providedIn: 'root' })
export class ChatHubService {
    private connection: signalR.HubConnection;
    message$ = new Subject<string>(); 

    chunk$ = new Subject<string>();
    done$  = new Subject<void>();


    constructor() {
        this.connection = new signalR.HubConnectionBuilder()
        .withUrl('/ai-chat')            // ← matches the hub endpoint
        .withAutomaticReconnect()
        .build();

        this.connection.on('ReceiveMessage', (msg: string) => {
        console.log('[SignalR] Received AI reply:', msg);
        this.message$.next(msg);        // ← pushes AI reply to subscribers
        });

    
        this.connection.on('ReceiveChunk', (chunk: string) => this.chunk$.next(chunk));
        this.connection.on('ReceiveDone',  ()              => this.done$.next());
    }
  
    async connect(): Promise<void> {
        if (this.connection.state === signalR.HubConnectionState.Disconnected) {
        await this.connection.start();
        }
    }

    async disconnect(): Promise<void> {
        await this.connection.stop();
    }

    async sendMessage(message: string): Promise<void> {
        // Assume it will invoke the SendMessage of ChatHub
        console.log(message);
        await this.connection.invoke('SendMessage', message);
    }
    
}