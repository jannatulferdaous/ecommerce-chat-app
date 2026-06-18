import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { CartService } from '../../services/cart.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <div class="auth-page container">
      <div class="card auth-card">
        <h2>Welcome back</h2>
        <p class="subtitle">Log in to chat, shop, and check out.</p>

        <form [formGroup]="form" (ngSubmit)="submit()">
          <label>Email</label>
          <input type="email" formControlName="email" placeholder="you@example.com" />

          <label>Password</label>
          <input type="password" formControlName="password" placeholder="••••••••" />

          <div class="error-text" *ngIf="error">{{ error }}</div>

          <button class="btn btn-primary full" type="submit" [disabled]="form.invalid || loading">
            {{ loading ? 'Logging in...' : 'Log in' }}
          </button>
        </form>

        <p class="switch">
          Don't have an account? <a routerLink="/register">Sign up</a>
        </p>
      </div>
    </div>
  `,
  styles: [`
    .auth-page {
      display: flex;
      justify-content: center;
      padding: 60px 16px;
    }
    .auth-card {
      width: 100%;
      max-width: 380px;
      padding: 32px;
    }
    h2 { margin: 0 0 4px; }
    .subtitle { margin: 0 0 20px; color: #5b6175; font-size: 14px; }
    form { display: flex; flex-direction: column; gap: 6px; }
    label { font-size: 13px; font-weight: 600; margin-top: 10px; }
    input {
      padding: 10px 12px;
      border: 1px solid #d9dcec;
      border-radius: 8px;
      font-size: 14px;
    }
    .full { width: 100%; margin-top: 18px; }
    .switch { margin-top: 16px; font-size: 13px; text-align: center; }
  `]
})
export class LoginComponent {
  form = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]]
  });

  loading = false;
  error = '';

  constructor(
    private fb: FormBuilder,
    private auth: AuthService,
    private cart: CartService,
    private router: Router
  ) {}

  submit(): void {
    if (this.form.invalid) return;
    this.loading = true;
    this.error = '';

    const { email, password } = this.form.getRawValue();

    this.auth.login(email!, password!).subscribe({
      next: () => {
        this.cart.refresh();
        this.router.navigate(['/']);
      },
      error: (err) => {
        this.loading = false;
        this.error = err?.error?.message || 'Login failed. Please check your credentials.';
      }
    });
  }
}
