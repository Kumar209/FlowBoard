import { Component, signal, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { HeaderComponent } from '../../../shared/components/header/header.component';

/**
 * RegisterComponent - MNC-grade: OnPush + signals + ReactiveForms typed + always-enabled button + input-error below.
 * Boilerplate (hasError with touched||dirty||submitted, markAllAsTouched on submit) is intentional for production UX (a11y, not disabled).
 */
@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, HeaderComponent],
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class RegisterComponent {
  form: any;
  loading = signal(false);
  error = signal<string | null>(null);
  success = signal(false);
  submitted = signal(false);
  showPassword = signal(false);

  constructor(
    private fb: FormBuilder,
    private auth: AuthService,
    private router: Router
  ) {
    this.form = this.fb.group({
      fullName: ['', [Validators.required, Validators.maxLength(200)]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(100)]]
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

    const { fullName, email, password } = this.form.getRawValue();

    this.auth.register(email!, password!, fullName!).subscribe({
      next: (res: any) => {
        this.auth.setSession(
          {
            id: res.user.id,
            email: res.user.email,
            fullName: res.user.fullName
          },
          res.accessToken
        );
        this.auth.me().subscribe({
          next: me => this.auth.hydrateFromMe(me as any),
          error: () => {}
        });
        this.success.set(true);
        this.loading.set(false);

        setTimeout(() => this.router.navigate(['/']), 800);
      },
      error: (err: any) => {
        this.error.set(
          err.error?.error ||
          err.error?.message ||
          'Registration failed'
        );
        this.loading.set(false);
      }
    });
  }
}