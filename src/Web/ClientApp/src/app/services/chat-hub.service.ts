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
    private botReplies = 0;
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
            .configureLogging(signalR.LogLevel.Information)
            .build();

        // Streaming: accumulate chunks into a live bot message
        let streamingId: number | null = null;
        let streamingText = '';

        this.connection.on('ReceiveChunk', (chunk: string) => {
            streamingText += chunk;
            if (streamingId === null) {
                // First chunk — replace typing indicator with live message
                streamingId = ++this.msgId;
                this.message$.next({ id: streamingId, text: streamingText, _streaming: true } as any);
            } else {
                // Subsequent chunks — update in place
                this.message$.next({ id: streamingId, text: streamingText, _streaming: true } as any);
            }
        });

        this.connection.on('ReceiveChunkDone', (fullText: string) => {
            // Finalise: emit the clean full response, bump reply counter
            const finalId = streamingId ?? ++this.msgId;
            this.message$.next({ id: finalId, text: fullText, _streaming: false } as any);
            streamingId = null;
            streamingText = '';

            this.botReplies++;
            if (this.botReplies === 2) {
                this.local$.next({
                    id: ++this.msgId,
                    author: this.bot,
                    text: 'Would you like to speak with someone from our team?',
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
                    map((raw: any) => ({
                        id: raw.id ?? ++this.msgId,
                        author: this.bot,
                        text: raw.text ?? raw,
                        timestamp: new Date(),
                        _streaming: raw._streaming ?? false,
                    } as Message & { _streaming: boolean }))
                )
            ).pipe(
                scan((acc: Message[], msg: any) => {
                    // Remove typing indicators unless this is a typing message
                    let base = msg.typing ? acc : acc.filter((m: any) => !m.typing);
                    // If same id exists (streaming update), replace in place
                    const existingIdx = base.findIndex(m => m.id === msg.id);
                    if (existingIdx >= 0) {
                        base = [...base.slice(0, existingIdx), msg, ...base.slice(existingIdx + 1)];
                    } else {
                        base = [...base, msg];
                    }
                    return base;
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
