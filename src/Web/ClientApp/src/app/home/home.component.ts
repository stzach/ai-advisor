import { Component, Inject, Signal, computed } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { HttpClient } from '@angular/common/http';
import { map } from 'rxjs/operators';
import { API_BASE_URL, UsersClient } from '../web-api-client';
import { ProductType, TransactionCategory, TransactionType } from '../enums';
import { ChatHubService } from '../services/chat-hub.service';

interface UserProductDto {
  productId: string;
  productName: string;
  productType: ProductType;
  availableBalance: number;
  cardNumber?: string;
  accountNumber?: string;
}

interface UserTransactionDto {
  transactionId: string;
  productName: string;
  transactionType: TransactionType;
  transactionCategory: TransactionCategory;
  amount: number;
  from?: string;
  to?: string;
  created: string;
}

interface AccountRow     { name: string; iban: string; balance: number; }
interface CardRow        { name: string; number: string; type: string; balance: number; }
interface TransactionRow { id: string; type: TransactionType; date: Date; from: string; to: string; amount: number; category: TransactionCategory | null; description: string; }
interface ExpenseRow     { category: string; amount: number; color: string; }

const CATEGORY_COLORS: Record<TransactionCategory, string> = {
  [TransactionCategory.Housing]:       '#c8102e',
  [TransactionCategory.Food]:          '#4a90d9',
  [TransactionCategory.Transport]:     '#2ecc71',
  [TransactionCategory.Entertainment]: '#f39c12',
  [TransactionCategory.Utilities]:     '#9b59b6',
  [TransactionCategory.Other]:         '#7f8c8d',
};

@Component({
  standalone: false,
  selector: 'app-home',
  templateUrl: './home.component.html',
})
export class HomeComponent {
  readonly TransactionType = TransactionType;

  readonly insights = [
    { icon: '💡', message: 'You could save €180/month by switching your utilities provider.',  cta: 'Explore options',      prompt: 'How can I reduce my utilities spending?' },
    { icon: '📊', message: 'You\'re spending 18% more on food compared to last month.',        cta: 'See breakdown',        prompt: 'Break down my food spending this month.' },
    { icon: '🏦', message: 'A savings account could earn you €312/year in interest.',          cta: 'Open savings account', prompt: 'How much can I earn by moving money to savings?' },
    { icon: '💳', message: 'You have €1,380 available credit across your cards.',              cta: 'View card offers',     prompt: 'What is my available credit and how should I use it?' },
  ];

  readonly username:     Signal<string>;
  readonly products:     Signal<UserProductDto[]>;
  readonly rawTxs:       Signal<UserTransactionDto[]>;
  readonly accounts:     Signal<AccountRow[]>;
  readonly cards:        Signal<CardRow[]>;
  readonly transactions: Signal<TransactionRow[]>;
  readonly expenses:     Signal<ExpenseRow[]>;
  readonly totalExpenses: Signal<number>;

  constructor(
    public  chatHub:      ChatHubService,
    private http:         HttpClient,
    private usersClient:  UsersClient,
    @Inject(API_BASE_URL) private baseUrl: string
  ) {
    this.username = toSignal(
      this.usersClient.infoGET().pipe(map(info => info.email.split('@')[0])),
      { initialValue: '' }
    );

    this.products = toSignal(
      this.http.get<UserProductDto[]>(`${this.baseUrl}/api/UserProducts`),
      { initialValue: [] }
    );

    this.rawTxs = toSignal(
      this.http.get<UserTransactionDto[]>(`${this.baseUrl}/api/UserTransactions`),
      { initialValue: [] }
    );

    this.accounts = computed(() =>
      this.products()
        .filter(p => p.productType === ProductType.Account)
        .map(p => ({ name: p.productName, iban: p.accountNumber ?? 'N/A', balance: p.availableBalance }))
    );

    this.cards = computed(() =>
      this.products()
        .filter(p => p.productType === ProductType.Card)
        .map(p => ({
          name:    p.productName,
          number:  p.cardNumber ?? '**** **** **** ****',
          type:    p.productName.toLowerCase().includes('credit') ? 'CREDIT' : 'DEBIT',
          balance: p.availableBalance,
        }))
    );

    this.transactions = computed(() =>
      this.rawTxs().map(tx => ({
        id:          tx.transactionId,
        type:        tx.transactionType,
        date:        new Date(tx.created),
        from:        tx.from ?? '',
        to:          tx.to ?? '',
        amount:      tx.amount,
        category:    tx.transactionCategory ?? null,
        description: tx.to ?? tx.productName,
      }))
    );

    this.expenses = computed(() => {
      const totals: Partial<Record<TransactionCategory, number>> = {};
      for (const tx of this.rawTxs()) {
        if (tx.transactionCategory) {
          totals[tx.transactionCategory] = (totals[tx.transactionCategory] ?? 0) + tx.amount;
        }
      }
      return (Object.entries(totals) as [TransactionCategory, number][]).map(([category, amount]) => ({
        category,
        amount,
        color: CATEGORY_COLORS[category] ?? '#95a5a6',
      }));
    });

    this.totalExpenses = computed(() => this.expenses().reduce((s, e) => s + e.amount, 0));
  }
}
