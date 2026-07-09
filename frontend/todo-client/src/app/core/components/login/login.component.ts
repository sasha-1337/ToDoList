import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { HttpErrorResponse } from '@angular/common/http';
import {environment} from "../../../../environments/environment";

import { AuthService } from '../../auth/services/auth.service';

declare var google: any;

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
  ],
  templateUrl: './login.component.html',
})
export class LoginComponent {
  private readonly GOOGLE_CLIENT_ID = environment.googleClientId;

  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly hidePassword = signal(true);

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]],
  });

  ngAfterViewInit(): void {
    // Ініціалізація Google Identity Services
    if (typeof google !== 'undefined') {
      google.accounts.id.initialize({
        client_id: this.GOOGLE_CLIENT_ID,
        // bind(this) обов'язковий, щоб зберегти контекст компонента
        callback: this.handleGoogleCredentialResponse.bind(this),
      });

      google.accounts.id.renderButton(
        document.getElementById('google-btn-container'),
        { theme: 'outline', size: 'large', width: '320' } // Налаштування стилю кнопки
      );
    } else {
      console.warn('Скрипт Google Identity Services не завантажено.');
    }
  }

  togglePasswordVisibility(): void {
    this.hidePassword.update((v) => !v);
  }

  onSubmit(): void {
    if (this.form.invalid || this.isSubmitting()) return;

    this.errorMessage.set(null);
    this.isSubmitting.set(true);

    const { email, password } = this.form.getRawValue();
    this.authService.login(email, password).subscribe({
      next: () => this.router.navigateByUrl('/'),
      error: (err: HttpErrorResponse) => {
        this.isSubmitting.set(false);
        // Підлаштуйте під реальний формат помилок вашого .NET-бекенда (напр. err.error.message)
        this.errorMessage.set(
          err.status === 401 ? 'Невірний email або пароль' : 'Щось пішло не так. Спробуйте ще раз',
        );
      },
    });
  }

  // Обробник успішної авторизації через Google
  private handleGoogleCredentialResponse(response: any): void {
    this.errorMessage.set(null);
    this.isSubmitting.set(true);

    // response.credential — це JWT (idToken), який видав Google
    this.authService.googleOAuth(response.credential).subscribe({
      next: () => this.router.navigateByUrl('/'),
      error: (err: HttpErrorResponse) => {
        this.isSubmitting.set(false);
        this.errorMessage.set('Помилка авторизації через Google. Спробуйте ще раз.');
      },
    });
  }
}