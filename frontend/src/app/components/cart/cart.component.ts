import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { CartService } from '../../services/cart.service';
import { OrderService } from '../../services/order.service';

@Component({
  selector: 'app-cart',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="container page">
      <h1>Your Cart</h1>

      <div class="card empty" *ngIf="cart.cart().items.length === 0">
        <p>Your cart is empty.</p>
      </div>

      <div class="card cart-card" *ngIf="cart.cart().items.length > 0">
        <div class="row header">
          <span class="col-name">Item</span>
          <span class="col-size">Size</span>
          <span class="col-qty">Qty</span>
          <span class="col-price">Price</span>
          <span class="col-action"></span>
        </div>

        <div class="row" *ngFor="let item of cart.cart().items">
          <span class="col-name">
            <img [src]="item.imageUrl" [alt]="item.productName" />
            <div>
              <div class="name">{{ item.productName }}</div>
              <div class="brand">{{ item.brand }}</div>
            </div>
          </span>
          <span class="col-size">{{ item.size }}</span>
          <span class="col-qty">{{ item.quantity }}</span>
          <span class="col-price">\${{ item.lineTotal.toFixed(2) }}</span>
          <span class="col-action">
            <button class="btn btn-danger" (click)="remove(item.cartItemId)">Remove</button>
          </span>
        </div>

        <div class="total-row">
          <span>Total</span>
          <span>\${{ cart.cart().total.toFixed(2) }}</span>
        </div>

        <div class="error-text" *ngIf="error">{{ error }}</div>

        <button class="btn btn-primary checkout-btn" (click)="checkout()" [disabled]="placing">
          {{ placing ? 'Placing order...' : "I'm ready to place my order" }}
        </button>
      </div>

      <div class="card success" *ngIf="placedOrder">
        <h3>🎉 Order #{{ placedOrder.id }} placed!</h3>
        <p>Total charged: \${{ placedOrder.totalAmount.toFixed(2) }} (simulated — no real payment was processed)</p>
        <a routerLink="/" class="btn btn-secondary">Continue shopping</a>
      </div>
    </div>
  `,
  styles: [`
    .page { padding: 24px 0 60px; }
    .cart-card { padding: 18px; }
    .row {
      display: grid;
      grid-template-columns: 2fr 0.6fr 0.6fr 0.8fr 0.8fr;
      align-items: center;
      gap: 10px;
      padding: 12px 0;
      border-bottom: 1px solid #f0f1f7;
    }
    .row.header {
      font-size: 12px;
      text-transform: uppercase;
      color: #8b8fa3;
      letter-spacing: 0.04em;
      border-bottom: 1px solid #eceef5;
    }
    .col-name { display: flex; align-items: center; gap: 10px; }
    .col-name img { width: 48px; height: 48px; object-fit: cover; border-radius: 8px; background: #f0f1f7; }
    .name { font-weight: 600; font-size: 14px; }
    .brand { font-size: 12px; color: #8b8fa3; }
    .col-action { text-align: right; }
    .total-row {
      display: flex;
      justify-content: space-between;
      font-weight: 700;
      font-size: 18px;
      padding: 16px 0;
    }
    .checkout-btn { width: 100%; padding: 14px; font-size: 15px; }
    .empty { padding: 40px; text-align: center; color: #8b8fa3; }
    .success { padding: 24px; text-align: center; margin-top: 18px; }
    .success h3 { margin-top: 0; }
  `]
})
export class CartComponent implements OnInit {
  error = '';
  placing = false;
  placedOrder: { id: number; totalAmount: number } | null = null;

  constructor(public cart: CartService, private orders: OrderService) {}

  ngOnInit(): void {
    this.cart.refresh();
  }

  remove(cartItemId: number): void {
    this.cart.removeItem(cartItemId).subscribe({
      error: () => (this.error = 'Could not remove this item. Please try again.')
    });
  }

  checkout(): void {
    this.error = '';
    this.placing = true;
    this.orders.checkout().subscribe({
      next: (order) => {
        this.placing = false;
        this.placedOrder = { id: order.id, totalAmount: order.totalAmount };
        this.cart.clearLocal();
      },
      error: (err) => {
        this.placing = false;
        this.error = err?.error?.message || 'Could not place your order. Please try again.';
      }
    });
  }
}
