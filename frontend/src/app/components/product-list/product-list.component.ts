import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ProductService } from '../../services/product.service';
import { CartService } from '../../services/cart.service';
import { AuthService } from '../../services/auth.service';
import { Product } from '../../models/models';

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="container page">
      <h1>Shop T-Shirts &amp; Pants</h1>

      <div class="filters">
        <button class="btn" [class.btn-primary]="category === undefined" [class.btn-secondary]="category !== undefined"
                (click)="setCategory(undefined)">All</button>
        <button class="btn" [class.btn-primary]="category === 'T-Shirt'" [class.btn-secondary]="category !== 'T-Shirt'"
                (click)="setCategory('T-Shirt')">T-Shirts</button>
        <button class="btn" [class.btn-primary]="category === 'Pants'" [class.btn-secondary]="category !== 'Pants'"
                (click)="setCategory('Pants')">Pants</button>
      </div>

      <div class="status" *ngIf="message">{{ message }}</div>

      <div class="grid">
        <div class="card product" *ngFor="let p of products">
          <img [src]="p.imageUrl" [alt]="p.name" />
          <div class="info">
            <div class="brand">{{ p.brand }}</div>
            <h3>{{ p.name }}</h3>
            <div class="price">\${{ p.price.toFixed(2) }}</div>

            <label class="size-label">Size</label>
            <select [(ngModel)]="selectedSize[p.id]" name="size-{{ p.id }}">
              <option *ngFor="let v of p.variants" [value]="v.size" [disabled]="v.stock === 0">
                {{ v.size }}{{ v.stock === 0 ? ' (out of stock)' : '' }}
              </option>
            </select>

            <button class="btn btn-primary add-btn" (click)="addToCart(p)">Add to cart</button>
          </div>
        </div>
      </div>

      <p *ngIf="products.length === 0 && !loading" class="empty">No products found.</p>
    </div>
  `,
  styles: [`
    .page { padding: 24px 0 60px; }
    h1 { margin-bottom: 8px; }
    .filters { display: flex; gap: 10px; margin-bottom: 16px; }
    .status {
      background: #eef2ff;
      color: #2f54eb;
      padding: 10px 14px;
      border-radius: 8px;
      margin-bottom: 16px;
      font-size: 14px;
    }
    .grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
      gap: 18px;
    }
    .product { overflow: hidden; display: flex; flex-direction: column; }
    .product img { width: 100%; height: 180px; object-fit: cover; background: #f0f1f7; }
    .info { padding: 14px; display: flex; flex-direction: column; gap: 6px; }
    .brand { font-size: 12px; color: #8b8fa3; text-transform: uppercase; letter-spacing: 0.05em; }
    h3 { margin: 0; font-size: 15px; line-height: 1.3; min-height: 38px; }
    .price { font-weight: 700; color: #2f54eb; }
    .size-label { font-size: 12px; font-weight: 600; margin-top: 4px; }
    select {
      padding: 8px;
      border-radius: 6px;
      border: 1px solid #d9dcec;
      font-size: 13px;
    }
    .add-btn { margin-top: 6px; }
    .empty { color: #8b8fa3; }
  `]
})
export class ProductListComponent implements OnInit {
  products: Product[] = [];
  category?: string;
  selectedSize: Record<number, string> = {};
  loading = false;
  message = '';

  constructor(
    private productService: ProductService,
    private cartService: CartService,
    private auth: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.load();
  }

  setCategory(category?: string): void {
    this.category = category;
    this.load();
  }

  load(): void {
    this.loading = true;
    this.productService.getAll(this.category).subscribe({
      next: (products) => {
        this.products = products;
        // Default each product's selected size to the first in-stock size
        for (const p of products) {
          const firstInStock = p.variants.find((v) => v.stock > 0);
          this.selectedSize[p.id] = firstInStock?.size ?? p.variants[0]?.size ?? 'M';
        }
        this.loading = false;
      },
      error: () => (this.loading = false)
    });
  }

  addToCart(product: Product): void {
    if (!this.auth.isLoggedIn()) {
      this.message = 'Please log in to add items to your cart.';
      this.router.navigate(['/login']);
      return;
    }

    const size = this.selectedSize[product.id];
    this.cartService.addItem(product.id, size, 1).subscribe({
      next: () => {
        this.message = `Added "${product.name}" (size ${size}) to your cart.`;
      },
      error: (err) => {
        this.message = err?.error?.message || 'Could not add this item to your cart.';
      }
    });
  }
}
