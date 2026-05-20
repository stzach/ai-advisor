import { Component, computed, Signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Subject, of } from 'rxjs';
import { map, startWith, switchMap, catchError, tap } from 'rxjs/operators';
import { ChatHubService } from '../services/chat-hub.service';
import { UserProductsClient, UserProductDto, UserTransactionsClient, UserTransactionDto, UsersClient } from '../web-api-client';
import { API_BASE_URL } from '../web-api-client';
import { Inject } from '@angular/core';

interface InsightDto { icon: string; message: string; cta: string; prompt: string; }

interface Expense   { category: string; amount: number; color: string; }

const CHART_COLORS = ['#4a90d9', '#2ecc71', '#f39c12', '#9b59b6', '#7f8c8d', '#1abc9c', '#e67e22'];

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
  private range$            = new BehaviorSubject<string>('month');
  private productsRefresh$  = new Subject<void>();
  private insightsLoading$  = new BehaviorSubject<boolean>(true);

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
  insights:        Signal<InsightDto[]>;
  insightsLoading: Signal<boolean>;

  constructor(
    public chatHub: ChatHubService,
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
        if (tx.transactionType === 'Loan') return false;
        if (tx.transactionType === 'Transfer') return !ownNumbers.has(tx.to ?? '');
        return true; // Payment
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
        category,
        amount,
        color: category === maxCat ? '#c8102e' : CHART_COLORS[i % CHART_COLORS.length],
      }));
    });

    this.totalExpenses = computed(() => this.expenses().reduce((s, e) => s + e.amount, 0));

    this.netWorth = computed(() => {
      const assets      = this.userProducts().filter(p => p.productType === 'Account').reduce((s, p) => s + (p.availableBalance ?? 0), 0);
      const liabilities = this.userProducts().filter(p => p.productType === 'Loan').reduce((s, p) => s + (p.availableBalance ?? 0), 0);
      return assets - liabilities;
    });

    this.insightsLoading = toSignal(this.insightsLoading$, { initialValue: true });

    this.insights = toSignal(
      this.range$.pipe(
        switchMap(range => {
          this.insightsLoading$.next(true);
          const { from, to } = this.toDateRange(range);
          return this.http.get<InsightDto[]>(`${this.baseUrl}/api/AiInsights`, {
            params: { from: from.toISOString(), to: to.toISOString() }
          }).pipe(
            startWith([] as InsightDto[]),
            catchError(() => of([] as InsightDto[])),
            tap({ complete: () => this.insightsLoading$.next(false) })
          );
        })
      ),
      { initialValue: [] as InsightDto[] }
    ) as Signal<InsightDto[]>;
  }

  showModal = false;
  modalTab: 'payment' | 'transfer' = 'payment';
  modalAmount: number | null = null;
  modalFromProductId = '';
  modalTo = '';
  modalDescription = '';
  submitting = false;
  submitError: string | null = null;

  productLabel(p: UserProductDto): string {
    const last4 = p.cardNumber?.slice(-4) ?? p.accountNumber?.slice(-4) ?? '0000';
    return `${p.productName} ···${last4}`;
  }

  openModal() {
    const products = this.payableProducts();
    this.modalTab           = 'payment';
    this.modalAmount        = null;
    this.modalFromProductId = products.length ? (products[0].productId ?? '') : '';
    this.modalTo            = '';
    this.modalDescription   = '';
    this.submitError        = null;
    this.showModal          = true;
  }

  closeModal() { this.showModal = false; }

  get selectedRange(): string { return this.range$.value; }

  setRange(range: string): void { this.range$.next(range); }

  rangeLabel(): string {
    const labels: Record<string, string> = {
      'month':   'This Month',
      '3months': 'Last 3 Months',
      '6months': 'Last 6 Months',
      'year':    'This Year',
    };
    return labels[this.range$.value] ?? '';
  }

  private toDateRange(range: string): { from: Date; to: Date } {
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

  searchQuery = '';
  searchResults: FinancialDocumentSearchResultDto[] = [];
  isSearching = false;

  searchDocuments(): void {
    if (!this.searchQuery.trim()) {
      this.searchResults = [];
      return;
    }

    this.isSearching = true;
    this.searchResults = [];

    this.http
      .post<FinancialDocumentSearchResultDto[]>(
        `${this.baseUrl}/api/financial-documents/search`,
        { query: this.searchQuery.trim(), topK: 5 }
      )
      .subscribe({
        next: results => {
          this.searchResults = results;
        },
        error: () => {
          this.searchResults = [];
        },
        complete: () => {
          this.isSearching = false;
        }
      });
  }

    
  submitTransaction() {
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
