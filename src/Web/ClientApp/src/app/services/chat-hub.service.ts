import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject, merge, Observable, of, BehaviorSubject } from 'rxjs';
import { map, scan, observeOn, shareReplay, startWith, switchMap } from 'rxjs/operators';
import { asyncScheduler } from 'rxjs';
import { Message, User, Action } from '@progress/kendo-angular-conversational-ui';

@Injectable({ providedIn: 'root' })
export class ChatHubService {
    private connection: signalR.HubConnection;

    readonly user: User = { id: 'user', name: 'You' };
    readonly bot: User  = { id: 'bot',  name: 'AI Assistant' };

    isOpen$ = new BehaviorSubject<boolean>(false);

    private msgId  = 0;
    private reset$   = new Subject<void>();
    private local$   = new Subject<Message>();
    private message$ = new Subject<string>();

    private readonly welcome: Message = {
        id: 0,
        author: this.bot,
        text: 'Hi! I\'m your AI financial advisor. What can I help you with today?',
        timestamp: new Date(),
        suggestedActions: [
            { type: 'reply', value: 'Analyze my spending this month' },
            { type: 'reply', value: 'Which product better matches my needs?' },
            { type: 'reply', value: 'What are my biggest expenses?' },
            { type: 'reply', value: 'Propose limits on my spendings?' },
        ] as Action[]
    };

    feed$: Observable<Message[]>;

    constructor() {
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl('/ai-chat')
            .withAutomaticReconnect()
            .build();

        this.connection.on('ReceiveMessage', (msg: string) => {
            console.log('[SignalR] Received AI reply:', msg);
            this.message$.next(msg);
        });

        this.feed$ = this.reset$.pipe(
            startWith(null as null),
            switchMap(() => merge(
                of(this.welcome),
                this.local$,
                this.message$.pipe(
                    map(text => ({ id: ++this.msgId, author: this.bot, text, timestamp: new Date() } as Message))
                )
            ).pipe(
                scan((acc: Message[], msg: Message) => {
                    const base = msg.typing ? acc : acc.filter(m => !m.typing);
                    return [...base, msg];
                }, [])
            )),
            observeOn(asyncScheduler),
            shareReplay(1)
        );
    }

    open(): void   { this.isOpen$.next(true); }
    close(): void  { this.isOpen$.next(false); }
    toggle(): void { this.isOpen$.next(!this.isOpen$.value); }
    clear(): void  { this.isOpen$.next(false); this.reset$.next(); }

    openWithPrompt(text: string): void {
        this.isOpen$.next(true);
        this.sendUserMessage(text);
    }

    sendUserMessage(text: string): void {
        const msg: Message = { id: Date.now(), author: this.user, text, timestamp: new Date() };
        this.local$.next(msg);
        this.local$.next({ id: 'typing', author: this.bot, typing: true } as any);
        this.sendMessage(text).catch(err => console.error('[Chat] sendMessage failed:', err));
    }

    pushLocal(msg: Message): void {
        this.local$.next(msg);
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
        await this.connection.invoke('SendMessage', message);
    }
}
