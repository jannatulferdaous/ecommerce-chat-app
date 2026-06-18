import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { OrderService } from '../../services/order.service';
import { Order } from '../../models/models';

@Component({
  selector: 'app-orders',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="container page">
      <h1>Your Orders</h1>

      <div class="card empty" *ngIf="orders.length === 0 && !loading">
        <p>You haven't placed any orders yet.</p>
        <a routerLink="/" class="btn btn-primary">Start shopping</a>
      </div>

      <div *ngFor="let order of orders" class="card order-card">
        <div class="order-header">
          <div>
            <span class="label">Order #{{ order.id }}</span>
            <span class="date">{{ order.orderDate | date: 'medium' }}</span>
          </div>
          <div class="status-badge">{{ order.status }}</div>
        </div>

        <div class="items">
          <div class="item-row" *ngFor="let item of order.items">
            <span class="item-name">{{ item.productName }}</span>
            <span class="item-detail">Size: {{ item.size }}</span>
            <span class="item-detail">Qty: {{ item.quantity }}</span>
            <span class="item-price">\${{ item.lineTotal.toFixed(2) }}</span>
          </div>
        </div>

        <div class="order-total">
          <span>Total</span>
          <span>\${{ order.totalAmount.toFixed(2) }}</span>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .page { padding: 24px 0 60px; }
    .order-card { margin-bottom: 18px; padding: 20px; }
    .order-header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      margin-bottom: 14px;
    }
    .label { font-weight: 700; display: block; margin-bottom: 2px; }
    .date { font-size: 12px; color: #8b8fa3; }
    .status-badge {
      background: #e6f4ea;
      color: #2e7d32;
      font-size: 12px;
      font-weight: 600;
      padding: 4px 10px;
      border-radius: 999px;
    }
    .items { border-top: 1px solid #eceef5; padding-top: 12px; }
    .item-row {
      display: flex;
      gap: 12px;
      align-items: center;
      padding: 6px 0;
      font-size: 14px;
    }
    .item-name { flex: 1; font-weight: 500; }
    .item-detail { color: #8b8fa3; font-size: 13px; }
    .item-price { font-weight: 600; color: #2f54eb; }
    .order-total {
      display: flex;
      justify-content: space-between;
      font-weight: 700;
      border-top: 1px solid #eceef5;
      margin-top: 10px;
      padding-top: 10px;
    }
    .empty { padding: 40px; text-align: center; color: #8b8fa3; }
    .empty .btn { margin-top: 14px; display: inline-block; text-decoration: none; }
  `]
})
export class OrdersComponent implements OnInit {
  orders: Order[] = [];
  loading = true;

  constructor(private orderService: OrderService) {}

  ngOnInit(): void {
    this.orderService.getMyOrders().subscribe({
      next: (orders) => {
        this.orders = orders;
        this.loading = false;
      },
      error: () => (this.loading = false)
    });
  }
}
