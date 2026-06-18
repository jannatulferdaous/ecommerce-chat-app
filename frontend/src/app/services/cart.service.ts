import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { Cart } from '../models/models';

const EMPTY_CART: Cart = { items: [], total: 0 };

@Injectable({ providedIn: 'root' })
export class CartService {
  // Shared reactive cart state so the navbar badge and cart page stay in sync,
  // including updates that come back from the chat widget.
  cart = signal<Cart>(EMPTY_CART);

  constructor(private http: HttpClient) {}

  refresh(): void {
    this.http.get<Cart>(`${environment.apiUrl}/cart`).subscribe({
      next: (cart) => this.cart.set(cart),
      error: () => this.cart.set(EMPTY_CART)
    });
  }

  addItem(productId: number, size: string, quantity: number = 1): Observable<Cart> {
    return this.http
      .post<Cart>(`${environment.apiUrl}/cart/items`, { productId, size, quantity })
      .pipe(tap((cart) => this.cart.set(cart)));
  }

  removeItem(cartItemId: number): Observable<Cart> {
    return this.http
      .delete<Cart>(`${environment.apiUrl}/cart/items/${cartItemId}`)
      .pipe(tap((cart) => this.cart.set(cart)));
  }

  /** Used by the chat widget to push a server-returned cart snapshot into shared state. */
  setCart(cart: Cart): void {
    this.cart.set(cart);
  }

  clearLocal(): void {
    this.cart.set(EMPTY_CART);
  }
}
