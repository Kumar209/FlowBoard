import { Component, signal, ChangeDetectionStrategy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { HeaderComponent } from '../../../shared/components/header/header.component';

/**
 * LoginComponent - MNC-grade: OnPush + inject() + signals (loading/error/submitted) + ReactiveForms with hasError(touched||dirty||submitted) + always-enabled button.
 * Why not simple? OnPush + signals gives fine-grained CD, not full tick. hasError with submitted ensures error shows on click submit (not disabled) + on blur.
 */
@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, HeaderComponent],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LoginComponent {
  form: any;
  loading = signal(false);
  error = signal<string | null>(null);
  submitted = signal(false);
  showPassword = signal(false);

  constructor(
    private fb: FormBuilder,
    private auth: AuthService,
    private router: Router
  ) {
    this.form = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(8)]]
    });
  }

  hasError(control: string, error: string): boolean {
    const c = this.form.get(control);
    return !!c && c.hasError(error) && (c.touched || c.dirty || this.submitted());
  }

  togglePassword(): void {
    this.showPassword.update(value => !value);
  }

  onSubmit(): void {
    this.submitted.set(true);
    this.form.markAllAsTouched();

    if (this.form.invalid) {
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    const { email, password } = this.form.getRawValue();

    this.auth.login(email!, password!).subscribe({
      next: (res: any) => {
        this.auth.setSession(
          {
            id: res.user.id,
            email: res.user.email,
            fullName: res.user.fullName
          },
          res.accessToken
        );
        // Hydrate memberships via me() so role-based UI works immediately (sidebar + board hide)
        this.auth.me().subscribe({
          next: me => { this.auth.hydrateFromMe(me as any); this.loading.set(false); this.router.navigate(['/']); },
          error: () => { this.loading.set(false); this.router.navigate(['/']); }
        });
        return;
      },
      error: (err: any) => {
        this.error.set(
          err.error?.error ||
          err.error?.message ||
          'Login failed - check email/password'
        );

        this.loading.set(false);
      }
    });
  }
}