import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { InsightsService, InsightDto } from '../services/insights.service';
import { ChatHubService } from '../services/chat-hub.service';

@Component({
  standalone: false,
  selector: 'app-insights',
  templateUrl: './insights.component.html',
})
export class InsightsComponent implements OnInit {
  selectedRange      = 'month';
  expandedInsight:   number | null = null;
  highlightedInsight: number | null = null;

  constructor(
    public insightsService: InsightsService,
    public chatHub: ChatHubService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.insightsService.markRead();

    // getCurrentNavigation() is null by the time ngOnInit runs — use history.state
    const state = history.state as
      { insights?: InsightDto[]; range?: string; highlightIndex?: number } | undefined;

    if (state?.insights?.length) {
      this.selectedRange = state.range ?? this.insightsService.activeRange();
      this.insightsService.applyState(state.insights, this.selectedRange);
      if (state.highlightIndex !== undefined) {
        this.highlightedInsight = state.highlightIndex;
        this.expandedInsight    = state.highlightIndex;
      }
    } else {
      this.selectedRange = this.insightsService.activeRange();
      this.insightsService.loadForRange(this.selectedRange);
    }
  }

  setRange(range: string): void {
    this.selectedRange = range;
    this.expandedInsight = null;
    this.insightsService.loadForRange(range);
  }

  toggleInsight(i: number): void {
    this.expandedInsight = this.expandedInsight === i ? null : i;
  }

  goBack(): void {
    this.router.navigate(['/']);
  }
}
