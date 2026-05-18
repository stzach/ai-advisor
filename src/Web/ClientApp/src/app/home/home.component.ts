import { Component, OnInit } from '@angular/core';
import { ChatHubService } from '../services/chat-hub.service';
import { UserProductsClient, UserProductDto, UserTransactionsClient, UserTransactionDto, UsersClient } from '../web-api-client';

@Component({
  standalone: false,
  selector: 'app-home',
  templateUrl: './home.component.html',
})
export class HomeComponent implements OnInit {
  constructor(
    public chatHub: ChatHubService,
    private productsClient: UserProductsClient,
    private transactionsClient: UserTransactionsClient,
    private usersClient: UsersClient
  ) {}

  username = '';
  userProducts: UserProductDto[] = [];
  transactions: UserTransactionDto[] = [];

  showModal = false;
  modalTab: 'payment' | 'transfer' = 'payment';
  modalAmount: number | null = null;
  modalFromProductId: string = '';
  modalTo: string = '';
  modalDescription: string = '';
  submitting = false;
  submitError: string | null = null;

  expenses: { category: string; amount: number; color: string }[] = [];

  insights = [
    { icon: '💡', message: 'You could save €180/month by switching your utilities provider.',  cta: 'Explore options',      prompt: 'How can I reduce my utilities spending?' },
    { icon: '📊', message: 'You\'re spending 18% more on food compared to last month.',        cta: 'See breakdown',        prompt: 'Break down my food spending this month.' },
    { icon: '🏦', message: 'A savings account could earn you €312/year in interest.',          cta: 'Open savings account', prompt: 'How much can I earn by moving money to savings?' },
    { icon: '💳', message: 'You have €1,380 available credit across your cards.',              cta: 'View card offers',     prompt: 'What is my available credit and how should I use it?' },
  ];

  ngOnInit() {
    this.loadData();
  }

  loadData() {
    this.usersClient.me().subscribe(profile => {
      this.username = profile.firstName ?? '';
    });
    this.productsClient.getUserProducts().subscribe(products => {
      this.userProducts = products;
    });
    this.transactionsClient.getUserTransactions().subscribe(txs => {
      this.transactions = txs;
    });
  }

  get accounts() {
    return this.userProducts.filter(p => p.productType === 'Account');
  }

  get cards() {
    return this.userProducts.filter(p => p.productType === 'Card');
  }

  get totalExpenses(): number {
    return this.expenses.reduce((s, e) => s + e.amount, 0);
  }

  productLabel(p: UserProductDto): string {
    const last4 = p.cardNumber?.slice(-4) ?? p.accountNumber?.slice(-4) ?? '0000';
    return `${p.productName} ···${last4}`;
  }

  openModal() {
    this.modalTab = 'payment';
    this.modalAmount = null;
    this.modalFromProductId = this.userProducts.length ? (this.userProducts[0].productId ?? '') : '';
    this.modalTo = '';
    this.modalDescription = '';
    this.submitError = null;
    this.showModal = true;
  }

  closeModal() {
    this.showModal = false;
  }

  submitTransaction() {
    if (!this.modalAmount || this.modalAmount <= 0 || !this.modalFromProductId) return;

    this.submitting = true;
    this.submitError = null;

    const product = this.userProducts.find(p => p.productId === this.modalFromProductId);
    const command: any = {
      productId: this.modalFromProductId,
      transactionType: this.modalTab === 'payment' ? 2 : 1,
      transactionCategory: 6,
      transactionDirection: 2,
      amount: this.modalAmount,
      from: product ? this.productLabel(product) : '',
      to: this.modalTo || undefined,
    };

    this.transactionsClient.createUserTransaction(command).subscribe({
      next: () => {
        this.submitting = false;
        this.closeModal();
        this.loadData();
      },
      error: () => {
        this.submitting = false;
        this.submitError = 'Transaction failed. Please try again.';
      }
    });
  }
}
