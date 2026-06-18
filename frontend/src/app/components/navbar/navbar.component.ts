import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { CartService } from '../../services/cart.service';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <header class="navbar">
      <div class="container nav-inner">
        <a routerLink="/" class="brand">🛍️ StyleChat</a>

        <nav class="nav-links">
          <a routerLink="/">Shop</a>
          <a routerLink="/cart" *ngIf="auth.isLoggedIn()">
            Cart
            <span class="badge" *ngIf="cart.cart().items.length > 0">{{ cart.cart().items.length }}</span>
          </a>
          <a routerLink="/orders" *ngIf="auth.isLoggedIn()">Orders</a>
        </nav>

        <div class="auth-area">
          <ng-container *ngIf="auth.isLoggedIn(); else loggedOut">
            <span class="user-name">Hi, {{ auth.currentUser()?.fullName }}</span>
            <button class="btn btn-secondary" (click)="logout()">Log out</button>
          </ng-container>
          <ng-template #loggedOut>
            <a routerLink="/login" class="btn btn-secondary">Log in</a>
            <a routerLink="/register" class="btn btn-primary">Sign up</a>
          </ng-template>
        </div>
      </div>
    </header>
  `,
  styles: [`
    .navbar {
      background: #fff;
      border-bottom: 1px solid #eceef5;
      position: sticky;
      top: 0;
      z-index: 50;
    }
    .nav-inner {
      display: flex;
      align-items: center;
      justify-content: space-between;
      height: 64px;
      gap: 24px;
    }
    .brand {
      font-size: 20px;
      font-weight: 800;
      text-decoration: none;
      color: #2f54eb;
    }
    .nav-links {
      display: flex;
      gap: 20px;
      flex: 1;
    }
    .nav-links a {
      text-decoration: none;
      color: #1f2430;
      font-weight: 500;
      position: relative;
    }
    .badge {
      position: absolute;
      top: -10px;
      right: -16px;
      background: #ff4d4f;
      color: #fff;
      border-radius: 999px;
      font-size: 11px;
      padding: 2px 6px;
      font-weight: 700;
    }
    .auth-area {
      display: flex;
      align-items: center;
      gap: 10px;
    }
    .user-name {
      font-size: 14px;
      color: #5b6175;
      margin-right: 4px;
    }
  `]
})
export class NavbarComponent implements OnInit {
  constructor(public auth: AuthService, public cart: CartService, private router: Router) {}

  ngOnInit(): void {
    if (this.auth.isLoggedIn()) {
      this.cart.refresh();
    }
  }

  logout(): void {
    this.auth.logout();
    this.cart.clearLocal();
    this.router.navigate(['/']);
  }
}
