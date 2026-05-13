import { Component, NgZone, OnInit, OnDestroy } from '@angular/core';
import { Message, User, SendMessageEvent } from '@progress/kendo-angular-conversational-ui';
import { Subscription } from 'rxjs';
import { ChatHubService } from '../services/chat-hub.service';

@Component({
  standalone: false,
  selector: 'app-chat',
  templateUrl: './chat.html'
})
export class ChatComponent implements OnInit, OnDestroy {
  messages: Message[] = [];

  readonly user: User = { id: 'user', name: 'You' };
  readonly bot: User  = { id: 'bot',  name: 'AI Assistant' };

  private sub!: Subscription;
  private msgId = 0;

  constructor(private chatHub: ChatHubService, private zone: NgZone) {}

  ngOnInit() {
    this.sub = this.chatHub.message$.subscribe(text => {
      this.zone.run(() => {
        this.messages = [
          ...this.messages.filter(m => !m.typing),
          { id: ++this.msgId, author: this.bot, text, timestamp: new Date() }
        ];
      });
    });
  }

  async onSendMessage(e: SendMessageEvent) {
    this.messages = [
      ...this.messages,
      e.message,
      { id: 'typing', author: this.bot, typing: true }
    ];
    try {
      await this.chatHub.sendMessage(e.message.text ?? '');
    } catch (err) {
      console.error('[Chat] sendMessage failed:', err);
      this.messages = this.messages.filter(m => !m.typing);
    }
  }

  ngOnDestroy() {
    this.sub?.unsubscribe();
  }
}