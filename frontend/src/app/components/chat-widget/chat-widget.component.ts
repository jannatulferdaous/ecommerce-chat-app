import {
  Component,
  OnInit,
  OnDestroy,
  AfterViewChecked,
  ViewChild,
  ElementRef,
  signal
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink, Router } from '@angular/router';
import { ChatService } from '../../services/chat.service';
import { AuthService } from '../../services/auth.service';
import { Product, CartItem } from '../../models/models';

@Component({
  selector: 'app-chat-widget',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <!-- Floating toggle button -->
    <button class="chat-toggle" (click)="toggleOpen()" [title]="open() ? 'Close chat' : 'Open StyleChat assistant'">
      <span *ngIf="!open()">💬</span>
      <span *ngIf="open()">✕</span>
    </button>

    <!-- Chat panel -->
    <div class="chat-panel card" [class.hidden]="!open()">
      <div class="chat-header">
        <span class="bot-avatar">🛍️</span>
        <div>
          <div class="bot-name">StyleChat</div>
          <div class="bot-status">Your personal shopping assistant</div>
        </div>
      </div>

      <!-- Message list -->
      <div class="messages" #messageContainer>
        <!-- Not logged in state -->
        <div *ngIf="!auth.isLoggedIn()" class="info-bubble">
          Please <a routerLink="/login" (click)="open.set(false)">log in</a> or
          <a routerLink="/register" (click)="open.set(false)">sign up</a>
          to start chatting.
        </div>

        <!-- Welcome message on first load -->
        <div *ngIf="auth.isLoggedIn() && chatService.messages().length === 0" class="bot-msg">
          <div class="bubble">
            Hi {{ auth.currentUser()?.fullName }}! 👋 I can help you browse t-shirts &amp; pants,
            add/remove items from your cart, or place an order. Try:
            <ul>
              <li>"Show me running t-shirts"</li>
              <li>"Add Nike t-shirt size L to my cart"</li>
              <li>"What's in my cart?"</li>
              <li>"I'm ready to place my order"</li>
            </ul>
          </div>
        </div>

        <!-- Persisted + live messages -->
        <ng-container *ngFor="let msg of chatService.messages(); let i = index">
          <!-- User bubble -->
          <div class="user-msg" *ngIf="msg.role === 'user'">
            <div class="bubble">{{ msg.content }}</div>
          </div>

          <!-- Bot bubble -->
          <div class="bot-msg" *ngIf="msg.role === 'assistant'">
            <div class="bubble" [innerHTML]="formatReply(msg.content)"></div>
          </div>

          <!-- If this assistant message came with product cards, render them -->
          <div class="product-cards" *ngIf="msg.role === 'assistant' && productsByIndex[i]?.length">
            <div class="product-card" *ngFor="let p of productsByIndex[i]">
              <img [src]="p.imageUrl" [alt]="p.name" />
              <div class="pc-info">
                <div class="pc-brand">{{ p.brand }}</div>
                <div class="pc-name">{{ p.name }}</div>
                <div class="pc-price">\${{ p.price.toFixed(2) }}</div>
                <div class="pc-sizes">
                  <span *ngFor="let v of p.variants"
                        class="size-chip"
                        [class.oos]="v.stock === 0"
                        [title]="v.stock === 0 ? 'Out of stock' : v.stock + ' in stock'">
                    {{ v.size }}
                  </span>
                </div>
              </div>
            </div>
          </div>
        </ng-container>

        <!-- Typing indicator -->
        <div class="bot-msg" *ngIf="loading">
          <div class="bubble typing">
            <span></span><span></span><span></span>
          </div>
        </div>
      </div>

      <!-- Input bar -->
      <form class="input-bar" (ngSubmit)="send()" *ngIf="auth.isLoggedIn()">
        <input
          #msgInput
          [(ngModel)]="draft"
          name="draft"
          placeholder="Ask me anything…"
          autocomplete="off"
          [disabled]="loading"
        />
        <button type="submit" [disabled]="!draft.trim() || loading">Send</button>
      </form>

      <!-- Quick-action chips (only shown when there are no messages yet) -->
      <div class="chips" *ngIf="auth.isLoggedIn() && chatService.messages().length === 0">
        <button class="chip" (click)="sendChip('Show me running t-shirts')">Running tees</button>
        <button class="chip" (click)="sendChip('Show me pants')">Pants</button>
        <button class="chip" (click)="sendChip('What is in my cart?')">My cart</button>
        <button class="chip" (click)="sendChip('Help')">Help</button>
      </div>
    </div>
  `,
  styles: [`
    /* ---- Floating button ---- */
    .chat-toggle {
      position: fixed;
      bottom: 28px;
      right: 28px;
      width: 58px;
      height: 58px;
      border-radius: 50%;
      background: #2f54eb;
      color: #fff;
      font-size: 24px;
      border: none;
      cursor: pointer;
      box-shadow: 0 4px 18px rgba(47, 84, 235, 0.38);
      z-index: 1000;
      display: flex;
      align-items: center;
      justify-content: center;
      transition: transform 0.15s ease;
    }
    .chat-toggle:hover { transform: scale(1.07); }

    /* ---- Panel ---- */
    .chat-panel {
      position: fixed;
      bottom: 96px;
      right: 28px;
      width: 380px;
      max-height: 600px;
      display: flex;
      flex-direction: column;
      z-index: 999;
      overflow: hidden;
      transition: opacity 0.2s ease, transform 0.2s ease;
    }
    .chat-panel.hidden {
      opacity: 0;
      pointer-events: none;
      transform: translateY(16px);
    }

    /* ---- Header ---- */
    .chat-header {
      background: #2f54eb;
      color: #fff;
      padding: 14px 16px;
      display: flex;
      align-items: center;
      gap: 10px;
    }
    .bot-avatar { font-size: 24px; }
    .bot-name { font-weight: 700; font-size: 15px; }
    .bot-status { font-size: 12px; opacity: 0.8; }

    /* ---- Messages ---- */
    .messages {
      flex: 1;
      overflow-y: auto;
      padding: 14px;
      display: flex;
      flex-direction: column;
      gap: 8px;
      background: #f6f7fb;
    }
    .user-msg { display: flex; justify-content: flex-end; }
    .bot-msg  { display: flex; justify-content: flex-start; }

    .bubble {
      max-width: 82%;
      padding: 10px 13px;
      border-radius: 14px;
      font-size: 14px;
      line-height: 1.5;
      white-space: pre-wrap;
      word-break: break-word;
    }
    .user-msg .bubble { background: #2f54eb; color: #fff; border-bottom-right-radius: 4px; }
    .bot-msg  .bubble { background: #fff; color: #1f2430; border-bottom-left-radius: 4px;
                        box-shadow: 0 1px 4px rgba(20,25,50,0.07); }
    .bot-msg .bubble ul { margin: 6px 0 0 16px; padding: 0; }
    .bot-msg .bubble li { margin-bottom: 2px; }

    .info-bubble {
      background: #fff8e1;
      color: #5a4a00;
      border-radius: 10px;
      padding: 12px 14px;
      font-size: 14px;
      text-align: center;
    }
    .info-bubble a { color: #2f54eb; font-weight: 600; }

    /* ---- Typing indicator ---- */
    .typing { display: flex; gap: 4px; padding: 12px 14px; }
    .typing span {
      width: 8px; height: 8px;
      background: #b0b4c8;
      border-radius: 50%;
      animation: bounce 1.2s infinite;
    }
    .typing span:nth-child(2) { animation-delay: 0.2s; }
    .typing span:nth-child(3) { animation-delay: 0.4s; }
    @keyframes bounce {
      0%, 60%, 100% { transform: translateY(0); }
      30% { transform: translateY(-6px); }
    }

    /* ---- Product cards from the bot ---- */
    .product-cards {
      display: flex;
      gap: 8px;
      overflow-x: auto;
      padding: 4px 0;
    }
    .product-card {
      min-width: 130px;
      max-width: 130px;
      background: #fff;
      border-radius: 10px;
      box-shadow: 0 1px 4px rgba(20,25,50,0.07);
      overflow: hidden;
      flex-shrink: 0;
    }
    .product-card img {
      width: 100%;
      height: 90px;
      object-fit: cover;
      background: #f0f1f7;
    }
    .pc-info { padding: 8px; }
    .pc-brand { font-size: 10px; color: #8b8fa3; text-transform: uppercase; letter-spacing: 0.04em; }
    .pc-name { font-size: 12px; font-weight: 600; line-height: 1.3; margin: 2px 0; }
    .pc-price { font-size: 13px; font-weight: 700; color: #2f54eb; margin-bottom: 4px; }
    .pc-sizes { display: flex; flex-wrap: wrap; gap: 3px; }
    .size-chip {
      font-size: 10px;
      padding: 2px 5px;
      border-radius: 4px;
      background: #eef2ff;
      color: #2f54eb;
      font-weight: 600;
    }
    .size-chip.oos {
      background: #f5f5f5;
      color: #bbb;
      text-decoration: line-through;
    }

    /* ---- Input bar ---- */
    .input-bar {
      display: flex;
      gap: 6px;
      padding: 10px 12px;
      background: #fff;
      border-top: 1px solid #eceef5;
    }
    .input-bar input {
      flex: 1;
      padding: 9px 12px;
      border: 1px solid #d9dcec;
      border-radius: 8px;
      font-size: 14px;
      outline: none;
    }
    .input-bar input:focus { border-color: #2f54eb; }
    .input-bar button {
      background: #2f54eb;
      color: #fff;
      border: none;
      border-radius: 8px;
      padding: 9px 16px;
      font-weight: 600;
      cursor: pointer;
      font-size: 14px;
    }
    .input-bar button:disabled { opacity: 0.5; cursor: not-allowed; }

    /* ---- Quick-action chips ---- */
    .chips {
      display: flex;
      flex-wrap: wrap;
      gap: 6px;
      padding: 8px 12px 12px;
      background: #fff;
      border-top: 1px solid #eceef5;
    }
    .chip {
      background: #eef2ff;
      color: #2f54eb;
      border: none;
      border-radius: 999px;
      padding: 6px 12px;
      font-size: 12px;
      font-weight: 600;
      cursor: pointer;
      transition: background 0.12s;
    }
    .chip:hover { background: #d6dfff; }

    @media (max-width: 440px) {
      .chat-panel { width: calc(100vw - 24px); right: 12px; bottom: 84px; }
    }
  `]
})
export class ChatWidgetComponent implements OnInit, OnDestroy, AfterViewChecked {
  @ViewChild('messageContainer') private messageContainer!: ElementRef<HTMLDivElement>;

  open = signal(false);
  draft = '';
  loading = false;

  /** Maps assistant message index → product array from that response */
  productsByIndex: Record<number, Product[]> = {};

  private shouldScroll = false;

  constructor(
    public chatService: ChatService,
    public auth: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    if (this.auth.isLoggedIn()) {
      this.chatService.loadHistory();
    }
  }

  ngOnDestroy(): void {}

  ngAfterViewChecked(): void {
    if (this.shouldScroll) {
      this.scrollToBottom();
      this.shouldScroll = false;
    }
  }

  toggleOpen(): void {
    this.open.update((v) => !v);
    if (this.open()) {
      this.shouldScroll = true;
    }
  }

  sendChip(text: string): void {
    this.draft = text;
    this.send();
  }

  send(): void {
    const text = this.draft.trim();
    if (!text || this.loading) return;

    if (!this.auth.isLoggedIn()) {
      this.router.navigate(['/login']);
      return;
    }

    this.draft = '';
    this.loading = true;
    this.shouldScroll = true;

    this.chatService.sendMessage(text).subscribe({
      next: (res) => {
        this.loading = false;
        this.shouldScroll = true;

        // Attach product cards to the latest assistant message index
        if (res.products && res.products.length > 0) {
          const msgIndex = this.chatService.messages().length - 1;
          this.productsByIndex[msgIndex] = res.products;
        }
      },
      error: () => {
        this.loading = false;
        // Insert a graceful error message so the chat doesn't just go silent
        this.chatService.messages.update((msgs) => [
          ...msgs,
          {
            role: 'assistant',
            content: "Sorry, I ran into an error processing that. Please try again."
          }
        ]);
        this.shouldScroll = true;
      }
    });
  }

  /** Converts \n-separated bot replies into simple HTML for display */
  formatReply(content: string): string {
    return content
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/\n/g, '<br/>');
  }

  private scrollToBottom(): void {
    try {
      const el = this.messageContainer?.nativeElement;
      if (el) el.scrollTop = el.scrollHeight;
    } catch {}
  }
}
