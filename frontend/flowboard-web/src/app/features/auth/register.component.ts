import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <div class="min-h-screen flex items-center justify-center bg-base-200 p-4">
      <div class="card w-full max-w-md bg-base-100 shadow-xl">
        <div class="card-body">
          <h2 class="card-title text-2xl justify-center">Create your FlowBoard account</h2>
          <p class="text-center text-sm opacity-70">Get a personal workspace instantly (OrgAdmin)</p>
          <form [formGroup]="form" (ngSubmit)="onSubmit()" class="space-y-4 mt-4">
            <div class="form-control w-full">
              <label class="label"><span class="label-text">Full Name</span></label>
              <input type="text" formControlName="fullName" placeholder="Prashant Kumar Verma" class="input input-bordered w-full" />
            </div>
            <div class="form-control w-full">
              <label class="label"><span class="label-text">Email</span></label>
              <input type="email" formControlName="email" placeholder="you@company.com" class="input input-bordered w-full" />
            </div>
            <div class="form-control w-full">
              <label class="label"><span class="label-text">Password (min 8)</span></label>
              <input type="password" formControlName="password" placeholder="••••••••" class="input input-bordered w-full" />
            </div>
            <div *ngIf="error()" class="alert alert-error py-2"><span class="text-sm">{{ error() }}</span></div>
            <div *ngIf="success()" class="alert alert-success py-2"><span class="text-sm">Account created - redirecting to dashboard...</span></div>
            <button type="submit" class="btn btn-primary w-full" [disabled]="form.invalid || loading()">
              <span *ngIf="loading()" class="loading loading-spinner loading-sm"></span>
              {{ loading() ? 'Creating...' : 'Create Account' }}
            </button>
          </form>
          <div class="text-center text-sm mt-4">
            Already have account? <a routerLink="/login" class="link link-primary">Sign in</a>
          </div>
        </div>
      </div>
    </div>
  `
})
export class RegisterComponent {
  form: any;
  loading = signal(false);
  error = signal<string | null>(null);
  success = signal(false);

  constructor(private fb: FormBuilder, private auth: AuthService, private router: Router) {
    this.form = this.fb.group({
      fullName: ['', [Validators.required, Validators.maxLength(200)]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(8)]]
    });
  }

  onSubmit() {
    if (this.form.invalid) return;
    this.loading.set(true); this.error.set(null);
    const { fullName, email, password } = this.form.getRawValue();
    this.auth.register(email!, password!, fullName!).subscribe({
      next: (res: any) => {
        this.auth.setSession({ id: res.user.id, email: res.user.email, fullName: res.user.fullName }, res.accessToken);
        this.success.set(true);
        this.loading.set(false);
        setTimeout(() => this.router.navigate(['/']), 800);
      },
      error: (err: any) => {
        this.error.set(err.error?.error || 'Registration failed');
        this.loading.set(false);
      }
    });
  }
}
