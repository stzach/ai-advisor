import { Component, computed, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { InsightCardComponent } from './insight-card.component';
import { InsightsService } from '../../services/insights.service';
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
    this.router.navigate(['/insights'], {
      state: {
        insights:       this.svc.insights(),
        range:          this.svc.activeRange(),
        highlightIndex: index,
      },
    });
  }
}
