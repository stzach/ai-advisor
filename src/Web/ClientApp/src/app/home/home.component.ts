import { Component, Inject, Signal, computed } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { Subject } from 'rxjs';
import { map, startWith, switchMap } from 'rxjs/operators';
import { HttpClient } from '@angular/common/http';
import { ChatHubService } from '../services/chat-hub.service';
import { API_BASE_URL, UserProductsClient, UserProductDto, UserTransactionsClient, UserTransactionDto, UsersClient } from '../web-api-client';

interface Expense { category: string; amount: number; color: string; }

const CHART_COLORS = ['#4a90d9', '#2ecc71', '#f39c12', '#9b59b6', '#7f8c8d', '#1abc9c', '#e67e22'];

@Component({
  standalone: false,
  selector: 'app-home',
  templateUrl: './home.component.html',
})
export class HomeComponent {
  private transRefresh$    = new Subject<void>();
  private productsRefresh$ = new Subject<void>();

  username:        Signal<string>;
  userProducts:    Signal<UserProductDto[]>;
  transactions:    Signal<UserTransactionDto[]>;
  accounts:        Signal<UserProductDto[]>;
  cards:           Signal<UserProductDto[]>;
  payableProducts: Signal<UserProductDto[]>;
  expenses:        Signal<Expense[]>;
  totalExpenses:   Signal<number>;

  showModal = false;
  modalTab: 'payment' | 'transfer' = 'payment';
  modalAmount: number | null = null;
  modalFromProductId = '';
  modalTo = '';
  modalDescription = '';
  submitting = false;
  submitError: string | null = null;

  readonly insights = [
    { icon: '💡', message: 'You could save €180/month by switching your utilities provider.',  cta: 'Explore options',      prompt: 'How can I reduce my utilities spending?' },
    { icon: '📊', message: 'You\'re spending 18% more on food compared to last month.',        cta: 'See breakdown',        prompt: 'Break down my food spending this month.' },
    { icon: '🏦', message: 'A savings account could earn you €312/year in interest.',          cta: 'Open savings account', prompt: 'How much can I earn by moving money to savings?' },
    { icon: '💳', message: 'You have €1,380 available credit across your cards.',              cta: 'View card offers',     prompt: 'What is my available credit and how should I use it?' },
  ];

  constructor(
    public  chatHub:            ChatHubService,
    private http:               HttpClient,
    private productsClient:     UserProductsClient,
    private transactionsClient: UserTransactionsClient,
    private usersClient:        UsersClient,
    @Inject(API_BASE_URL) private baseUrl: string
  ) {
    this.username = toSignal(
      this.usersClient.me().pipe(map(p => p.firstName || (p as any).email || '')),
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
      this.transRefresh$.pipe(
        startWith(null as null),
        switchMap(() => this.transactionsClient.getUserTransactions())
      ),
      { initialValue: [] as UserTransactionDto[] }
    );

    this.accounts        = computed(() => this.userProducts().filter(p => p.productType === 'Account'));
    this.cards           = computed(() => this.userProducts().filter(p => p.productType === 'Card'));
    this.payableProducts = computed(() => this.userProducts().filter(p => p.productType === 'Account' || p.productType === 'Card'));

    this.expenses = computed<Expense[]>(() => {
      const txs = this.transactions();
      if (!txs.length) return [];

      const summed = txs.reduce((acc, tx) => {
        const cat = tx.transactionCategory ?? 'Other';
        acc[cat] = (acc[cat] ?? 0) + (tx.amount ?? 0);
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
  }

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

  submitTransaction() {
    if (!this.modalAmount || this.modalAmount <= 0 || !this.modalFromProductId) return;

    this.submitting  = true;
    this.submitError = null;

    const product = this.payableProducts().find(p => p.productId === this.modalFromProductId);
    const command: any = {
      productId:            this.modalFromProductId,
      transactionType:      this.modalTab === 'payment' ? 2 : 1,
      transactionCategory:  6,
      transactionDirection: 2,
      amount:               this.modalAmount,
      from:                 product ? this.productLabel(product) : '',
      to:                   this.modalTo || undefined,
    };

    this.http.post(`${this.baseUrl}/api/UserTransactions`, command).subscribe({
      next: () => {
        this.submitting = false;
        this.closeModal();
        this.transRefresh$.next();
        this.productsRefresh$.next();
      },
      error: () => {
        this.submitting  = false;
        this.submitError = 'Transaction failed. Please try again.';
      }
    });
  }
}
