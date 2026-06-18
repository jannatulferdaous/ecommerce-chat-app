import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { ChatMessage, ChatResponse } from '../models/models';
import { CartService } from './cart.service';

@Injectable({ providedIn: 'root' })
export class ChatService {
  messages = signal<ChatMessage[]>([]);

  constructor(private http: HttpClient, private cartService: CartService) {}

  loadHistory(): void {
    this.http.get<ChatMessage[]>(`${environment.apiUrl}/chat/history`).subscribe({
      next: (history) => this.messages.set(history),
      error: () => this.messages.set([])
    });
  }

  sendMessage(text: string): Observable<ChatResponse> {
    // Optimistically show the user's message immediately
    this.messages.update((msgs) => [...msgs, { role: 'user', content: text }]);

    return this.http.post<ChatResponse>(`${environment.apiUrl}/chat/message`, { message: text }).pipe(
      tap((res) => {
        this.messages.update((msgs) => [...msgs, { role: 'assistant', content: res.reply }]);

        // Keep the cart badge/page in sync if the bot modified the cart
        if (res.cart) {
          this.cartService.setCart(res.cart);
        }
      })
    );
  }

  reset(): void {
    this.messages.set([]);
  }
}
