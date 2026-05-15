import { Component } from '@angular/core';
import { ChatHubService } from '../services/chat-hub.service';

@Component({
  standalone: false,
  selector: 'app-home',
  templateUrl: './home.component.html',
})
export class HomeComponent {
  constructor(public chatHub: ChatHubService) {}
  username = 'Theodoros';

  expenses = [
    { category: 'Housing',       amount: 1200, color: '#c8102e' },
    { category: 'Food',          amount: 450,  color: '#4a90d9' },
    { category: 'Transport',     amount: 280,  color: '#2ecc71' },
    { category: 'Entertainment', amount: 150,  color: '#f39c12' },
    { category: 'Utilities',     amount: 200,  color: '#9b59b6' },
    { category: 'Other',         amount: 120,  color: '#7f8c8d' },
  ];

  accounts = [
    { name: 'Current Account', iban: 'GR16 0140 1250 **** **** 0000', balance: 3420.50 },
    { name: 'Savings Account', iban: 'GR16 0140 1250 **** **** 0012', balance: 12750.00 },
  ];

  cards = [
    { name: 'Visa Debit',        number: '**** **** **** 4521', type: 'DEBIT',  limit: 5000,  spent: 1850 },
    { name: 'Mastercard Credit', number: '**** **** **** 9032', type: 'CREDIT', limit: 3000,  spent: 620  },
  ];

  transactions = [
    { type: 'transfer',  date: new Date('2025-05-10'), from: 'Current Account', to: 'Savings Account',     amount: -500.00,  category: null,          description: 'Transfer to Savings'       },
    { type: 'payment',   date: new Date('2025-05-09'), from: 'Visa Debit',      to: 'Sklavenitis S.A.',   amount: -86.40,   category: 'Food',        description: 'Supermarket'               },
    { type: 'payment',   date: new Date('2025-05-08'), from: 'Current Account', to: 'DEI',                amount: -94.00,   category: 'Utilities',   description: 'Electricity bill'          },
    { type: 'credit',    date: new Date('2025-05-07'), from: 'Employer Ltd.',   to: 'Current Account',    amount: +2800.00, category: null,          description: 'Salary – May 2025'         },
    { type: 'payment',   date: new Date('2025-05-06'), from: 'Mastercard',      to: 'Netflix',            amount: -14.99,   category: 'Entertainment', description: 'Streaming subscription'  },
    { type: 'payment',   date: new Date('2025-05-05'), from: 'Visa Debit',      to: 'Shell Station',      amount: -65.20,   category: 'Transport',   description: 'Fuel'                      },
    { type: 'payment',   date: new Date('2025-05-04'), from: 'Current Account', to: 'Landlord GR',        amount: -1200.00, category: 'Housing',     description: 'Rent – May 2025'           },
    { type: 'payment',   date: new Date('2025-05-03'), from: 'Visa Debit',      to: 'Mikel Coffee',       amount: -8.60,    category: 'Food',        description: 'Coffee & snack'            },
  ];

  insights = [
    { icon: '💡', message: 'You could save €180/month by switching your utilities provider.',  cta: 'Explore options',      prompt: 'How can I reduce my utilities spending?' },
    { icon: '📊', message: 'You\'re spending 18% more on food compared to last month.',        cta: 'See breakdown',        prompt: 'Break down my food spending this month.' },
    { icon: '🏦', message: 'A savings account could earn you €312/year in interest.',          cta: 'Open savings account', prompt: 'How much can I earn by moving money to savings?' },
    { icon: '💳', message: 'You have €1,380 available credit across your cards.',              cta: 'View card offers',     prompt: 'What is my available credit and how should I use it?' },
  ];


  get totalExpenses(): number {
    return this.expenses.reduce((s, e) => s + e.amount, 0);
  }

  spentPercent(card: { limit: number; spent: number }): number {
    return Math.round((card.spent / card.limit) * 100);
  }
}
