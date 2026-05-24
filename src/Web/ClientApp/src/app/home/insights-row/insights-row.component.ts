import { Component, computed, inject, output } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { InsightCardComponent } from './insight-card.component';
import { InsightsService, InsightDto } from '../../services/insights.service';
import { DashboardInsight } from './insights-row.types';

@Component({
  standalone:  true,
  selector:    'app-insights-row',
  imports:     [RouterLink, InsightCardComponent],
  templateUrl: './insights-row.component.html',
  styleUrl:    './insights-row.component.scss',
})
export class InsightsRowComponent {
  private svc    = inject(InsightsService);
  private router = inject(Router);

  loading   = this.svc.insightsLoading;
  skeletons = this.svc.insightSkeletons;

  insightSelected = output<InsightDto>();

  cards = computed<DashboardInsight[]>(() =>
    this.svc.insights().map(ins => ({
      id:       ins.title.toLowerCase().replace(/\s+/g, '-'),
      category: ins.category,
      icon:     ins.icon,
      tag:      ins.title,
      cta:      ins.cta,
    }))
  );

  onCtaClick(index: number): void {
    this.insightSelected.emit(this.svc.insights()[index]);
  }
}
