import { Component, ElementRef } from '@angular/core';
import { Message, User, SendMessageEvent, ExecuteActionEvent } from '@progress/kendo-angular-conversational-ui';
import { Observable } from 'rxjs';
import { tap } from 'rxjs/operators';
import { ChatHubService } from '../services/chat-hub.service';

@Component({
  standalone: false,
  selector: 'app-chat',
  templateUrl: './chat.html'
})
export class ChatComponent {
  readonly user: User = { id: 'user', name: 'You' };
  feed: Observable<Message[]>;

  constructor(private chatHub: ChatHubService, private el: ElementRef) {
    this.feed = chatHub.feed$.pipe(
      tap(() => setTimeout(() => this.scrollToBottom()))
    );
  }

  sendMessage(e: SendMessageEvent): void {
    this.chatHub.sendUserMessage(e.message.text ?? '');
  }

  executeAction(e: ExecuteActionEvent): void {
    e.preventDefault();
    this.chatHub.sendUserMessage(e.action.value);
  }

  private scrollToBottom(): void {
    const list: HTMLElement | null = this.el.nativeElement.querySelector('.k-message-list');
    if (list) list.scrollTop = list.scrollHeight;
  }
}
