import { Injectable, signal, computed, Inject } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';
import { API_BASE_URL } from '../web-api-client';

export interface InsightDto { icon: string; message: string; cta: string; prompt: string; }

@Injectable({ providedIn: 'root' })
export class InsightsService {
  private _insights  = signal<InsightDto[]>([]);
  private _loading   = signal(false);
  private _hasUnread = signal(false);
  private activeEventSource: EventSource | null = null;

  readonly insights         = this._insights.asReadonly();
  readonly insightsLoading  = this._loading.asReadonly();
  readonly hasUnread        = this._hasUnread.asReadonly();
  readonly insightSkeletons = computed(() =>
    Array.from({ length: Math.max(0, 4 - this._insights().length) }, (_, i) => i)
  );

  constructor(
    private router: Router,
    @Inject(API_BASE_URL) private baseUrl: string
  ) {
    this.router.events.pipe(
      filter(e => e instanceof NavigationEnd)
    ).subscribe((e: any) => {
      if ((e.urlAfterRedirects as string).startsWith('/insights')) {
        this._hasUnread.set(false);
      }
    });
  }

  load(from: Date, to: Date): void {
    this.activeEventSource?.close();
    this._insights.set([]);
    this._loading.set(true);

    const url = `${this.baseUrl}/api/AiInsights/stream`
      + `?from=${encodeURIComponent(from.toISOString())}`
      + `&to=${encodeURIComponent(to.toISOString())}`;

    const es = new EventSource(url, { withCredentials: true });
    this.activeEventSource = es;

    es.onmessage = (e) => {
      try {
        const insight: InsightDto = JSON.parse(e.data);
        this._insights.update(list => [...list, insight]);
        if (this._insights().length >= 4) this._loading.set(false);
      } catch { /* ignore malformed */ }
    };

    es.addEventListener('done', () => {
      this._loading.set(false);
      if (this._insights().length > 0 && !this.router.url.startsWith('/insights')) {
        this._hasUnread.set(true);
      }
      es.close();
      this.activeEventSource = null;
    });

    es.onerror = () => {
      this._loading.set(false);
      es.close();
      this.activeEventSource = null;
    };
  }

  markRead(): void { this._hasUnread.set(false); }

  toDateRange(range: string): { from: Date; to: Date } {
    const to = new Date();
    let from: Date;
    switch (range) {
      case '3months': from = new Date(); from.setMonth(from.getMonth() - 3);       break;
      case '6months': from = new Date(); from.setMonth(from.getMonth() - 6);       break;
      case 'year':    from = new Date(); from.setFullYear(from.getFullYear() - 1); break;
      default:        from = new Date(to.getFullYear(), to.getMonth(), 1);
    }
    return { from, to };
  }

  rangeLabel(range: string): string {
    const labels: Record<string, string> = {
      'month':   'This Month',
      '3months': 'Last 3 Months',
      '6months': 'Last 6 Months',
      'year':    'This Year',
    };
    return labels[range] ?? '';
  }
}
