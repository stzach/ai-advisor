import { Component, OnInit, OnDestroy } from '@angular/core';
import { InsightsService } from '../services/insights.service';
import { ChatHubService } from '../services/chat-hub.service';
import { VoiceService } from '../services/voice.service';

@Component({
  standalone: false,
  selector: 'app-insights',
  templateUrl: './insights.component.html',
})
export class InsightsComponent implements OnInit, OnDestroy {
  selectedRange    = 'month';
  expandedInsight: number | null = null;

  constructor(
    public insightsService: InsightsService,
    public chatHub: ChatHubService,
    public voice: VoiceService
  ) {}

  ngOnInit(): void {
    this.insightsService.markRead();
    const { from, to } = this.insightsService.toDateRange(this.selectedRange);
    this.insightsService.load(from, to);
  }

  setRange(range: string): void {
    this.selectedRange = range;
    this.expandedInsight = null;
    const { from, to } = this.insightsService.toDateRange(range);
    this.insightsService.load(from, to);
  }

  toggleInsight(i: number): void {
    this.expandedInsight = this.expandedInsight === i ? null : i;
  }

  speakInsight(insight: { message: string; prompt: string }): void {
    if (this.voice.currentSpeakingKey$.value === insight.message) {
      this.voice.stopSpeaking();
    } else {
      this.voice.speak(insight.message + '. ' + insight.prompt, insight.message);
    }
  }

  ngOnDestroy(): void {
    this.voice.stopSpeaking();
  }
}
