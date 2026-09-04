import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <div class="min-h-screen flex items-center justify-center bg-base-200 p-4">
      <div class="card w-full max-w-md bg-base-100 shadow-xl">
        <div class="card-body">
          <h2 class="card-title text-2xl justify-center">Welcome back to FlowBoard</h2>
          <p class="text-center text-sm opacity-70">Enterprise Project Management SaaS</p>
          <form [formGroup]="form" (ngSubmit)="onSubmit()" class="space-y-4 mt-4">
            <div class="form-control w-full">
              <label class="label"><span class="label-text">Email</span></label>
              <input type="email" formControlName="email" placeholder="you@company.com" class="input input-bordered w-full" />
            </div>
            <div class="form-control w-full">
              <label class="label"><span class="label-text">Password</span></label>
              <input type="password" formControlName="password" placeholder="••••••••" class="input input-bordered w-full" />
            </div>
            <div *ngIf="error()" class="alert alert-error py-2"><span class="text-sm">{{ error() }}</span></div>
            <button type="submit" class="btn btn-primary w-full" [disabled]="form.invalid || loading()">
              <span *ngIf="loading()" class="loading loading-spinner loading-sm"></span>
              {{ loading() ? 'Signing in...' : 'Sign In' }}
            </button>
          </form>
          <div class="text-center text-sm mt-4">
            No account? <a routerLink="/register" class="link link-primary">Create one</a>
          </div>
          <div class="text-center text-xs opacity-50 mt-2">JWT 15m + Refresh 7d via HttpOnly cookie • Single DB flowboard [identity]</div>
        </div>
      </div>
    </div>
  `
})
export class LoginComponent {
  form: any;
  loading = signal(false);
  error = signal<string | null>(null);

  constructor(private fb: FormBuilder, private auth: AuthService, private router: Router) {
    this.form = this.fb.group({ email: ['', [Validators.required, Validators.email]], password: ['', Validators.required] });
  }

  onSubmit() {
    if (this.form.invalid) return;
    this.loading.set(true); this.error.set(null);
    const { email, password } = this.form.getRawValue();
    this.auth.login(email!, password!).subscribe({
      next: (res: any) => {
        this.auth.setSession({ id: res.user.id, email: res.user.email, fullName: res.user.fullName }, res.accessToken);
        this.loading.set(false);
        this.router.navigate(['/']);
      },
      error: (err: any) => {
        this.error.set(err.error?.error || 'Login failed');
        this.loading.set(false);
      }
    });
  }
}
