import { Component, signal, ChangeDetectionStrategy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { HeaderComponent } from '../../../shared/components/header/header.component';
import { SuperAdminService } from '../../../core/services/superadmin.service';
import { injectQuery } from '@tanstack/angular-query-experimental';
import { firstValueFrom } from 'rxjs';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, HeaderComponent],
  templateUrl: './login.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LoginComponent {
  private sa = inject(SuperAdminService);

  maintenanceQuery = injectQuery(() => ({
    queryKey: ['platform-maintenance'] as const,
    queryFn: () => firstValueFrom(this.sa.getPlatformMaintenance()),
    staleTime: 30 * 1000
  }));

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

        this.auth.me().subscribe({
          next: me => {
            this.auth.hydrateFromMe(me as any);
            this.loading.set(false);
            this.router.navigate(this.auth.isSuperAdmin() ? ['/superadmin'] : ['/']);
          },
          error: () => {
            this.loading.set(false);
            this.router.navigate(this.auth.isSuperAdmin() ? ['/superadmin'] : ['/']);
          }
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