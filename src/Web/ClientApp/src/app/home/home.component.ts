import { Component, Inject, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { API_BASE_URL, UsersClient } from '../web-api-client';
import { ProductType, TransactionCategory, TransactionType } from '../enums';
import { ChatHubService } from '../services/chat-hub.service';

interface UserProductDto {
  id: number;
  productId: string;
  productName: string;
  productDescription?: string;
  productType: ProductType;
  availableBalance: number;
  isActive: boolean;
  cardNumber?: string;
  accountNumber?: string;
}

interface UserTransactionDto {
  transactionId: string;
  productId: string;
  productName: string;
  transactionType: TransactionType;
  transactionCategory: TransactionCategory;
  amount: number;
  from?: string;
  to?: string;
  created: string;
}

@Component({
  standalone: false,
  selector: 'app-home',
  templateUrl: './home.component.html',
})
export class HomeComponent implements OnInit {
  username = '';

  expenses: { category: string; amount: number; color: string }[] = [];
  accounts: { name: string; iban: string; balance: number }[] = [];
  cards: { name: string; number: string; type: string; balance: number }[] = [];
  transactions: { type: TransactionType; date: Date; from: string; to: string; amount: number; category: TransactionCategory | null; description: string }[] = [];

  readonly TransactionType = TransactionType;

  insights = [
    { icon: '💡', message: 'You could save €180/month by switching your utilities provider.',  cta: 'Explore options',      prompt: 'How can I reduce my utilities spending?' },
    { icon: '📊', message: 'You\'re spending 18% more on food compared to last month.',        cta: 'See breakdown',        prompt: 'Break down my food spending this month.' },
    { icon: '🏦', message: 'A savings account could earn you €312/year in interest.',          cta: 'Open savings account', prompt: 'How much can I earn by moving money to savings?' },
    { icon: '💳', message: 'You have €1,380 available credit across your cards.',              cta: 'View card offers',     prompt: 'What is my available credit and how should I use it?' },
  ];

  private readonly categoryColors: Record<TransactionCategory, string> = {
    [TransactionCategory.Housing]:       '#c8102e',
    [TransactionCategory.Food]:          '#4a90d9',
    [TransactionCategory.Transport]:     '#2ecc71',
    [TransactionCategory.Entertainment]: '#f39c12',
    [TransactionCategory.Utilities]:     '#9b59b6',
    [TransactionCategory.Other]:         '#7f8c8d',
  };

  constructor(
    public chatHub: ChatHubService,
    private http: HttpClient,
    private usersClient: UsersClient,
    @Inject(API_BASE_URL) private baseUrl: string
  ) {}

  ngOnInit(): void {
    this.usersClient.infoGET().subscribe(info => {
      this.username = info.email.split('@')[0];
    });
    this.loadProducts();
    this.loadTransactions();
  }

  private loadProducts(): void {
    this.http.get<UserProductDto[]>(`${this.baseUrl}/api/UserProducts`).subscribe(products => {
      this.accounts = products
        .filter(p => p.productType === ProductType.Account)
        .map(p => ({
          name:    p.productName,
          iban:    p.accountNumber ?? 'N/A',
          balance: p.availableBalance,
        }));

      this.cards = products
        .filter(p => p.productType === ProductType.Card)
        .map(p => ({
          name:    p.productName,
          number:  p.cardNumber ?? '**** **** **** ****',
          type:    p.productName.toLowerCase().includes('credit') ? 'CREDIT' : 'DEBIT',
          balance: p.availableBalance,
        }));
    });
  }

  private loadTransactions(): void {
    this.http.get<UserTransactionDto[]>(`${this.baseUrl}/api/UserTransactions`).subscribe(txs => {
      this.transactions = txs.map(tx => ({
        type:        tx.transactionType,
        date:        new Date(tx.created),
        from:        tx.from ?? '',
        to:          tx.to ?? '',
        amount:      tx.amount,
        category:    tx.transactionCategory ?? null,
        description: tx.to ?? tx.productName,
      }));

      this.buildExpenses(txs);
    });
  }

  private buildExpenses(txs: UserTransactionDto[]): void {
    const totals: Partial<Record<TransactionCategory, number>> = {};
    for (const tx of txs) {
      if (tx.amount < 0 && tx.transactionCategory) {
        totals[tx.transactionCategory] = (totals[tx.transactionCategory] ?? 0) + Math.abs(tx.amount);
      }
    }
    this.expenses = (Object.entries(totals) as [TransactionCategory, number][]).map(([category, amount]) => ({
      category,
      amount,
      color: this.categoryColors[category] ?? '#95a5a6',
    }));
  }

  get totalExpenses(): number {
    return this.expenses.reduce((s, e) => s + e.amount, 0);
  }
}
