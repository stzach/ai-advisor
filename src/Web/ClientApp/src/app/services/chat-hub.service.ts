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
    readonly bot: User  = { id: 'bot',  name: 'AI Virtual Assistant' };

    isOpen$ = new BehaviorSubject<boolean>(false);

    private msgId  = 0;
    private botReplies = 0;
    private reset$   = new Subject<void>();
    private local$   = new Subject<Message>();
    private message$ = new Subject<string>();

    private readonly welcome: Message = {
        id: 0,
        author: this.bot,
        text: 'Hello! I am the Alpha Bank digital assistant. How can I help you? Select one of the options below or simply type what you are looking for.',
        timestamp: new Date(),
        suggestedActions: [
            { type: 'reply', value: 'Immediate Help' },
            { type: 'reply', value: 'Accounts' },
            { type: 'reply', value: 'Cards' },
            { type: 'reply', value: 'Loans' },
            { type: 'reply', value: 'Insurance' },
            { type: 'reply', value: 'Bonus Rewards Programme' },
            { type: 'reply', value: 'Contact' },
        ] as Action[]
    };

    feed$: Observable<Message[]>;

    constructor() {
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl('/ai-chat')
            .withAutomaticReconnect()
            .configureLogging(signalR.LogLevel.Information)
            .build();
        this.connection.on('ReceiveMessage', (msg: string) => {
            console.log('[SignalR] Received AI reply:', msg);
            this.message$.next(msg);
             this.botReplies++;
            if (this.botReplies === 2) {
                this.local$.next({
                    id: ++this.msgId,
                    author: this.bot,
                    text: 'Get in touch with us',
                    timestamp: new Date(),
                    isEscalation: true
                } as any);
            }
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
    clear(): void  { this.isOpen$.next(false); this.botReplies = 0; this.reset$.next(); }
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
