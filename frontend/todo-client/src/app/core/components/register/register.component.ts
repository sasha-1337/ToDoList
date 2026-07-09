import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { HttpErrorResponse } from '@angular/common/http';

import { AuthService } from '../../auth/services/auth.service';

/** Валідатор: newPassword і confirmPassword мають збігатись */
function passwordsMatchValidator(control: AbstractControl): ValidationErrors | null {
  const password = control.get('password')?.value;
  const confirm = control.get('confirmPassword')?.value;
  return password && confirm && password !== confirm ? { passwordsMismatch: true } : null;
}

type RegisterStep = 'form' | 'verify';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, MatFormFieldModule, MatInputModule, MatButtonModule],
  templateUrl: './register.component.html',
})
export class RegisterComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly step = signal<RegisterStep>('form');
  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  /** Email, на який надіслано код — показуємо його на кроці 2 і використовуємо у verifyEmail() */
  readonly registeredEmail = signal('');

  readonly registerForm = this.fb.nonNullable.group(
    {
      email: ['', [Validators.required, Validators.email]],
      username: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(30)]],
      password: ['', [Validators.required, Validators.minLength(8)]],
      confirmPassword: ['', [Validators.required]],
    },
    { validators: passwordsMatchValidator },
  );

  readonly verifyForm = this.fb.nonNullable.group({
    code: ['', [Validators.required, Validators.minLength(4)]],
  });

  onRegisterSubmit(): void {
    if (this.registerForm.invalid || this.isSubmitting()) return;

    this.errorMessage.set(null);
    this.isSubmitting.set(true);

    const { email, username, password, confirmPassword } = this.registerForm.getRawValue();
    this.authService.register(email, username, password, confirmPassword).subscribe({
      next: () => {
        this.registeredEmail.set(email);
        this.step.set('verify');
        this.isSubmitting.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.isSubmitting.set(false);
        this.errorMessage.set(
          err.status === 409 ? 'Користувач з таким email вже існує' : 'Не вдалось зареєструватись. Спробуйте ще раз',
        );
      },
    });
  }

  onVerifySubmit(): void {
    if (this.verifyForm.invalid || this.isSubmitting()) return;

    this.errorMessage.set(null);
    this.isSubmitting.set(true);

    const { code } = this.verifyForm.getRawValue();
    this.authService.verifyEmail(this.registeredEmail(), code).subscribe({
      next: () => this.router.navigateByUrl('/'), // користувач вже автоматично залогований
      error: () => {
        this.isSubmitting.set(false);
        this.errorMessage.set('Невірний або прострочений код підтвердження');
      },
    });
  }

  /** Повернутись і виправити дані форми (напр. якщо помилились в email) */
  backToForm(): void {
    this.step.set('form');
    this.errorMessage.set(null);
  }
}