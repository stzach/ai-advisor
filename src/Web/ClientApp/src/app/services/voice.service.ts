import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class VoiceService {
  isListening$       = new BehaviorSubject<boolean>(false);
  isSpeaking$        = new BehaviorSubject<boolean>(false);
  currentSpeakingKey$ = new BehaviorSubject<string | null>(null);

  private recognition: any;
  private readonly SR: any = (window as any).SpeechRecognition
                           || (window as any).webkitSpeechRecognition;

  readonly supported = !!this.SR;

  listen(): Observable<string> {
    return new Observable(observer => {
      if (!this.supported) { observer.error('SpeechRecognition not supported'); return () => {}; }

      this.recognition?.stop();
      this.recognition = new this.SR();
      this.recognition.lang            = 'en-US';
      this.recognition.interimResults  = false;
      this.recognition.maxAlternatives = 1;

      this.recognition.onstart  = () => this.isListening$.next(true);
      this.recognition.onend    = () => this.isListening$.next(false);
      this.recognition.onerror  = (e: any) => { this.isListening$.next(false); observer.error(e); };
      this.recognition.onresult = (e: any) => {
        observer.next(e.results[0][0].transcript as string);
        observer.complete();
      };

      this.recognition.start();
      return () => this.recognition?.stop();
    });
  }

  stopListening(): void {
    this.recognition?.stop();
  }

  speak(text: string, key: string = text): void {
    window.speechSynthesis.cancel();
    const utterance = new SpeechSynthesisUtterance(this.stripForSpeech(text));
    utterance.rate  = 1;
    utterance.pitch = 1;
    utterance.onstart = () => { this.isSpeaking$.next(true);  this.currentSpeakingKey$.next(key); };
    utterance.onend   = () => { this.isSpeaking$.next(false); this.currentSpeakingKey$.next(null); };
    utterance.onerror = () => { this.isSpeaking$.next(false); this.currentSpeakingKey$.next(null); };
    window.speechSynthesis.speak(utterance);
  }

  stopSpeaking(): void {
    window.speechSynthesis.cancel();
    this.isSpeaking$.next(false);
    this.currentSpeakingKey$.next(null);
  }

  private stripForSpeech(text: string): string {
    return text
      .replace(/#+\s/g, '')
      .replace(/\*\*(.*?)\*\*/g, '$1')
      .replace(/\*(.*?)\*/g, '$1')
      .replace(/`([^`]+)`/g, '$1')
      .replace(/\[([^\]]+)\]\([^)]+\)/g, '$1')
      .replace(/<[^>]*>/g, '')
      .replace(/[_~>#\-]/g, '')
      .replace(/\s+/g, ' ')
      .trim();
  }
}
