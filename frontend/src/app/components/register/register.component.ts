import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <div class="auth-page container">
      <div class="card auth-card">
        <h2>Create your account</h2>
        <p class="subtitle">Sign up to save your cart and chat history.</p>

        <form [formGroup]="form" (ngSubmit)="submit()">
          <label>Full name</label>
          <input type="text" formControlName="fullName" placeholder="Jane Doe" />

          <label>Email</label>
          <input type="email" formControlName="email" placeholder="you@example.com" />

          <label>Password</label>
          <input type="password" formControlName="password" placeholder="At least 6 characters" />

          <div class="error-text" *ngIf="error">{{ error }}</div>

          <button class="btn btn-primary full" type="submit" [disabled]="form.invalid || loading">
            {{ loading ? 'Creating account...' : 'Sign up' }}
          </button>
        </form>

        <p class="switch">
          Already have an account? <a routerLink="/login">Log in</a>
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
export class RegisterComponent {
  form = this.fb.group({
    fullName: ['', [Validators.required, Validators.minLength(2)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]]
  });

  loading = false;
  error = '';

  constructor(private fb: FormBuilder, private auth: AuthService, private router: Router) {}

  submit(): void {
    if (this.form.invalid) return;
    this.loading = true;
    this.error = '';

    const { fullName, email, password } = this.form.getRawValue();

    this.auth.register(fullName!, email!, password!).subscribe({
      next: () => this.router.navigate(['/']),
      error: (err) => {
        this.loading = false;
        this.error = err?.error?.message || 'Registration failed. Please try again.';
      }
    });
  }
}
