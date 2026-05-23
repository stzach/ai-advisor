import { Component, input } from '@angular/core';
import { InsightCategory } from './insights-row.types';

@Component({
  standalone: true,
  selector: 'app-insight-orb',
  template: `
    <div class="orb" aria-hidden="true" [attr.data-category]="category()">
      <span class="orb-icon">{{ icon() }}</span>
    </div>
  `,
  styleUrl: './insight-orb.component.scss',
})
export class InsightOrbComponent {
  category = input.required<InsightCategory>();
  icon     = input.required<string>();
}
