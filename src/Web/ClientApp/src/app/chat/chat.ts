import { Component, ElementRef, OnDestroy } from '@angular/core';
import { Message, User, SendMessageEvent, ExecuteActionEvent } from '@progress/kendo-angular-conversational-ui';
import { Observable } from 'rxjs';
import { tap } from 'rxjs/operators';
import { ChatHubService } from '../services/chat-hub.service';
import { VoiceService } from '../services/voice.service';

@Component({
  standalone: false,
  selector: 'app-chat',
  templateUrl: './chat.html'
})
export class ChatComponent implements OnDestroy {
  readonly user: User = { id: 'user', name: 'You' };
  feed: Observable<Message[]>;

  constructor(
    private chatHub: ChatHubService,
    public voice: VoiceService,
    private el: ElementRef
  ) {
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

  toggleMic(): void {
    if (this.voice.isListening$.value) {
      this.voice.stopListening();
      return;
    }
    this.voice.listen().subscribe({
      next: transcript => this.chatHub.sendUserMessage(transcript),
      error: err => console.error('[Voice]', err)
    });
  }

  speak(messageId: string | number, text: string): void {
    const key = String(messageId);
    if (this.voice.currentSpeakingKey$.value === key) {
      this.voice.stopSpeaking();
    } else {
      this.voice.speak(text, key);
    }
  }

  ngOnDestroy(): void {
    this.voice.stopListening();
    this.voice.stopSpeaking();
  }

  private scrollToBottom(): void {
    const list: HTMLElement | null = this.el.nativeElement.querySelector('.k-message-list');
    if (list) list.scrollTop = list.scrollHeight;
  }
}
