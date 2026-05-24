import { Component, computed, effect, Signal, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Subject, of } from 'rxjs';
import { Router } from '@angular/router';
import { map, startWith, switchMap, catchError, shareReplay } from 'rxjs/operators';
import { Inject } from '@angular/core';

import { ChatHubService } from '../services/chat-hub.service';
import { InsightsService, InsightDto } from '../services/insights.service';
import { UserProductsClient, UserProductDto, UserTransactionsClient, UserTransactionDto, UsersClient } from '../web-api-client';
import { API_BASE_URL } from '../web-api-client';

interface ProductRecommendationDto { productName: string; redirectUri: string; reason: string; }
interface Expense { category: string; amount: number; color: string; }

const CHART_COLORS = ['#0ea5e9', '#10b981', '#8b5cf6', '#f59e0b', '#06b6d4', '#64748b', '#a78bfa'];

interface FinancialDocumentSearchResultDto {
  id: string;
  content: string;
  sourceFileName: string;
  sectionHeading: string;
  relevanceScore: number;
  chunkIndex: number;
  totalChunks: number;
}

@Component({
  standalone: false,
  selector: 'app-home',
  templateUrl: './home.component.html',
})
export class HomeComponent {
  private range$           = new BehaviorSubject<string>('month');
  private productsRefresh$ = new Subject<void>();

  username:        Signal<string>;
  userProducts:    Signal<UserProductDto[]>;
  transactions:    Signal<UserTransactionDto[]>;
  accounts:        Signal<UserProductDto[]>;
  cards:           Signal<UserProductDto[]>;
  loans:           Signal<UserProductDto[]>;
  payableProducts: Signal<UserProductDto[]>;
  expenses:        Signal<Expense[]>;
  totalExpenses:   Signal<number>;
  netWorth:        Signal<number>;
  productRecommendations: Signal<ProductRecommendationDto[]>;
  isLoadingRecommendations = true;

  // ── Advisor collapsible card ──────────────────────────────────────────────
  advisorExpanded = signal(false);

  // ── Rotating loading status phrases ──────────────────────────────────────
  private readonly _loadingPhrases = [
    'Reading your transactions…',
    'Spotting patterns…',
    'Ranking opportunities…',
    'Personalising insights…',
  ];
  loadingPhraseIndex = signal(0);
  loadingPhrase = computed(() => this._loadingPhrases[this.loadingPhraseIndex()]);
  private _phraseTimer: ReturnType<typeof setInterval> | null = null;

  // ── Insight detail modal ──────────────────────────────────────────────────
  selectedInsight = signal<InsightDto | null>(null);

  openInsightModal(insight: InsightDto): void {
    this.selectedInsight.set(insight);
  }

  closeInsightModal(): void {
    this.selectedInsight.set(null);
  }

  // ── Toast ──────────────────────────────────────────────────────────────────
  showToast = signal(false);
  private _toastShownForCurrentLoad = false;
  private _toastTimer: ReturnType<typeof setTimeout> | null = null;

  constructor(
    public chatHub: ChatHubService,
    public insightsService: InsightsService,
    private router: Router,
    private productsClient: UserProductsClient,
    private transactionsClient: UserTransactionsClient,
    private usersClient: UsersClient,
    private http: HttpClient,
    @Inject(API_BASE_URL) private baseUrl: string
  ) {
    this.username = toSignal(
      this.usersClient.me().pipe(map(p => p.firstName ?? '')),
      { initialValue: '' }
    );

    this.userProducts = toSignal(
      this.productsRefresh$.pipe(
        startWith(null as null),
        switchMap(() => this.productsClient.getUserProducts())
      ),
      { initialValue: [] as UserProductDto[] }
    );

    this.transactions = toSignal(
      this.range$.pipe(
        switchMap(range => {
          const { from, to } = this.toDateRange(range);
          return this.transactionsClient.getUserTransactions(from, to);
        })
      ),
      { initialValue: [] as UserTransactionDto[] }
    );

    this.accounts        = computed(() => this.userProducts().filter(p => p.productType === 'Account'));
    this.cards           = computed(() => this.userProducts().filter(p => p.productType === 'Card'));
    this.loans           = computed(() => this.userProducts().filter(p => p.productType === 'Loan'));
    this.payableProducts = computed(() => this.userProducts().filter(p => p.productType === 'Account' || p.productType === 'Card'));

    this.expenses = computed<Expense[]>(() => {
      const ownNumbers = new Set(
        this.userProducts().flatMap(p => [p.accountNumber, p.cardNumber]).filter(Boolean) as string[]
      );
      const txs = this.transactions().filter(tx => {
        if (tx.transactionDirection !== 'Outgoing') return false;
        if (tx.transactionType === 'Transfer') return !ownNumbers.has(tx.to ?? '');
        return true;
      });
      if (!txs.length) return [];
      const summed = txs.reduce((acc, tx) => {
        const cat = tx.transactionCategory ?? 'Other';
        acc[cat] = (acc[cat] ?? 0) + Math.abs(tx.amount ?? 0);
        return acc;
      }, {} as Record<string, number>);
      const entries = Object.entries(summed);
      const maxCat  = entries.reduce((a, b) => a[1] > b[1] ? a : b)[0];
      return entries.map(([category, amount], i) => ({
        category, amount,
        color: category === maxCat ? '#2563eb' : CHART_COLORS[i % CHART_COLORS.length],
      }));
    });

    this.totalExpenses = computed(() => this.expenses().reduce((s, e) => s + e.amount, 0));
    this.netWorth = computed(() =>
      this.userProducts()
        .filter(p => p.productType === 'Account')
        .reduce((s, p) => s + (p.availableBalance ?? 0), 0)
    );

    const recommendations$ = this.http.get<ProductRecommendationDto[]>(`${this.baseUrl}/api/ProductRecommendations`).pipe(
      catchError(() => of([] as ProductRecommendationDto[])),
      shareReplay({ bufferSize: 1, refCount: true })
    );
    this.productRecommendations = toSignal(recommendations$, { initialValue: [] as ProductRecommendationDto[] });
    recommendations$.subscribe({
      next:  () => this.isLoadingRecommendations = false,
      error: () => this.isLoadingRecommendations = false
    });

    // Kick off initial insights load and reload when range changes
    this.range$.subscribe(range => {
      this._toastShownForCurrentLoad = false;
      this.insightsService.loadForRange(range);
    });

    // Toast: fires once per load cycle when insights finish streaming
    effect(() => {
      const loading = this.insightsService.insightsLoading();
      const count   = this.insightsService.insights().length;
      if (loading) {
        this._toastShownForCurrentLoad = false;
        return;
      }
      if (!this._toastShownForCurrentLoad && count > 0) {
        this._toastShownForCurrentLoad = true;
        this.showToast.set(true);
        if (this._toastTimer) clearTimeout(this._toastTimer);
        this._toastTimer = setTimeout(() => this.showToast.set(false), 5000);
      }
    });

    // Expand advisor card immediately when loading starts, keep expanded when done
    effect(() => {
      const loading = this.insightsService.insightsLoading();
      const count   = this.insightsService.insights().length;
      if (loading || count > 0) {
        this.advisorExpanded.set(true);
      }
      // Start / stop rotating phrases
      if (loading) {
        this.loadingPhraseIndex.set(0);
        if (!this._phraseTimer) {
          this._phraseTimer = setInterval(() => {
            this.loadingPhraseIndex.update(i => (i + 1) % this._loadingPhrases.length);
          }, 1500);
        }
      } else {
        if (this._phraseTimer) { clearInterval(this._phraseTimer); this._phraseTimer = null; }
      }
    });
  }

  dismissToast(): void {
    this.showToast.set(false);
    if (this._toastTimer) clearTimeout(this._toastTimer);
  }

  goToInsights(): void {
    this.dismissToast();
    this.router.navigate(['/insights'], {
      state: { insights: this.insightsService.insights(), range: this.selectedRange }
    });
  }

  goToInsightDetail(index: number): void {
    this.router.navigate(['/insights'], {
      state: { insights: this.insightsService.insights(), range: this.selectedRange, highlightIndex: index }
    });
  }


  // ── Collapsible sections ──────────────────────────────────────────────────
  accountsExpanded = signal(true);
  cardsExpanded    = signal(true);
  loansExpanded    = signal(true);

  accountsTotal = computed(() =>
    this.accounts().reduce((s, a) => s + (a.availableBalance ?? 0), 0)
  );
  cardsAvailable = computed(() =>
    this.cards().reduce((s, c) => s + (c.availableBalance ?? 0), 0)
  );
  loansTotal = computed(() =>
    this.loans().reduce((s, l) => s + (l.availableBalance ?? 0), 0)
  );

  // ── Transaction pagination & filtering ────────────────────────────────────
  private readonly TX_PAGE_SIZE = 10;
  private txDisplayCount = signal(this.TX_PAGE_SIZE);
  txTypeFilter  = signal<string>('all');
  txSearchQuery = signal<string>('');

  filteredTransactions = computed(() => {
    let txs = this.transactions();
    const type = this.txTypeFilter();
    if (type !== 'all') txs = txs.filter(tx => tx.transactionType === type);
    const q = this.txSearchQuery().toLowerCase().trim();
    if (q) txs = txs.filter(tx =>
      tx.productName?.toLowerCase().includes(q) ||
      tx.from?.toLowerCase().includes(q) ||
      tx.to?.toLowerCase().includes(q) ||
      tx.transactionCategory?.toLowerCase().includes(q)
    );
    return txs;
  });

  displayedTransactions = computed(() => this.filteredTransactions().slice(0, this.txDisplayCount()));
  hasMoreTransactions   = computed(() => this.txDisplayCount() < this.filteredTransactions().length);
  nextBatchSize         = computed(() =>
    Math.min(this.TX_PAGE_SIZE, this.filteredTransactions().length - this.txDisplayCount())
  );

  loadMoreTransactions(): void { this.txDisplayCount.update(n => n + this.TX_PAGE_SIZE); }
  setTxFilter(type: string): void { this.txTypeFilter.set(type); this.txDisplayCount.set(this.TX_PAGE_SIZE); }
  setTxSearch(q: string):    void { this.txSearchQuery.set(q);   this.txDisplayCount.set(this.TX_PAGE_SIZE); }

  // ── Modal ──────────────────────────────────────────────────────────────────
  showModal = false;
  modalTab: 'payment' | 'transfer' = 'payment';
  modalAmount: number | null = null;
  modalFromProductId = '';
  modalTo = '';
  modalDescription = '';
  submitting  = false;
  submitError: string | null = null;

  productLabel(p: UserProductDto): string {
    const last4 = p.cardNumber?.slice(-4) ?? p.accountNumber?.slice(-4) ?? '0000';
    return `${p.productName} ···${last4}`;
  }

  openModal(): void {
    const products = this.payableProducts();
    this.modalTab           = 'payment';
    this.modalAmount        = null;
    this.modalFromProductId = products.length ? (products[0].productId ?? '') : '';
    this.modalTo            = '';
    this.modalDescription   = '';
    this.submitError        = null;
    this.showModal          = true;
  }

  closeModal(): void { this.showModal = false; }

  get selectedRange(): string { return this.range$.value; }

  setRange(range: string): void {
    this.range$.next(range);
    this.txDisplayCount.set(this.TX_PAGE_SIZE);
    this.txTypeFilter.set('all');
    this.txSearchQuery.set('');
  }

  rangeLabel(): string {
    return this.insightsService.rangeLabel(this.range$.value);
  }

  private toDateRange(range: string): { from: Date; to: Date } {
    return this.insightsService.toDateRange(range);
  }

  // ── Document search ────────────────────────────────────────────────────────
  searchQuery   = '';
  searchResults: FinancialDocumentSearchResultDto[] = [];
  isSearching   = false;

  searchDocuments(): void {
    if (!this.searchQuery.trim()) { this.searchResults = []; return; }
    this.isSearching = true;
    this.searchResults = [];
    this.http
      .post<FinancialDocumentSearchResultDto[]>(
        `${this.baseUrl}/api/financial-documents/search`,
        { query: this.searchQuery.trim(), topK: 5 }
      )
      .subscribe({
        next:     results => { this.searchResults = results; },
        error:    ()      => { this.searchResults = []; },
        complete: ()      => { this.isSearching = false; }
      });
  }

  // ── Transactions ───────────────────────────────────────────────────────────
  submitTransaction(): void {
    if (!this.modalAmount || this.modalAmount <= 0 || !this.modalFromProductId) return;
    this.submitting  = true;
    this.submitError = null;
    const product = this.userProducts().find(p => p.productId === this.modalFromProductId);
    const command: any = {
      productId:            this.modalFromProductId,
      transactionType:      this.modalTab === 'payment' ? 2 : 1,
      transactionCategory:  6,
      transactionDirection: 2,
      amount: this.modalAmount,
      from:   product ? this.productLabel(product) : '',
      to:     this.modalTo || undefined,
    };
    this.transactionsClient.createUserTransaction(command).subscribe({
      next: () => {
        this.submitting = false;
        this.closeModal();
        this.range$.next(this.range$.value);
        this.productsRefresh$.next();
      },
      error: () => {
        this.submitting  = false;
        this.submitError = 'Transaction failed. Please try again.';
      }
    });
  }
}
