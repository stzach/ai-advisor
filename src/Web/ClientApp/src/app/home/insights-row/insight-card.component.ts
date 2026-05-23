import { Component, input, output } from '@angular/core';
import { InsightOrbComponent } from './insight-orb.component';
import { DashboardInsight } from './insights-row.types';

@Component({
  standalone:  true,
  selector:    'app-insight-card',
  imports:     [InsightOrbComponent],
  templateUrl: './insight-card.component.html',
  styleUrl:    './insight-card.component.scss',
})
export class InsightCardComponent {
  insight   = input.required<DashboardInsight>();
  animDelay = input<number>(0);
  ctaClick  = output<void>();
}
