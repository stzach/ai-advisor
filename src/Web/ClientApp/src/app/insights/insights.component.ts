import { Component, OnInit } from '@angular/core';
import { InsightsService } from '../services/insights.service';
import { ChatHubService } from '../services/chat-hub.service';

@Component({
  standalone: false,
  selector: 'app-insights',
  templateUrl: './insights.component.html',
})
export class InsightsComponent implements OnInit {
  selectedRange    = 'month';
  expandedInsight: number | null = null;

  constructor(
    public insightsService: InsightsService,
    public chatHub: ChatHubService
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
}
