import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { HttpErrorResponse } from '@angular/common/http';

import { AuthService } from '../../auth/services/auth.service';

type Step = 'form' | 'verify';

@Component({
  selector: 'app-remove-account-dialog',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule],
  templateUrl: './remove-account-dialog.component.html',
})
export class RemoveAccountDialogComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  readonly dialogRef = inject(MatDialogRef<RemoveAccountDialogComponent, boolean>);

  readonly step = signal<Step>('form');
  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly passwordForm = this.fb.nonNullable.group({
    password: ['', [Validators.required]],
  });

  readonly codeForm = this.fb.nonNullable.group({
    code: ['', [Validators.required, Validators.minLength(4)]],
  });

  onRequestSubmit(): void {
    if (this.passwordForm.invalid || this.isSubmitting()) return;

    this.errorMessage.set(null);
    this.isSubmitting.set(true);

    const { password } = this.passwordForm.getRawValue();
    this.authService.requestRemoveAccount(password).subscribe({
      next: () => {
        this.step.set('verify');
        this.isSubmitting.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.isSubmitting.set(false);
        this.errorMessage.set(err.status === 401 ? 'Пароль невірний' : 'Не вдалось надіслати код');
      },
    });
  }

  onConfirmSubmit(): void {
    if (this.codeForm.invalid || this.isSubmitting()) return;

    this.errorMessage.set(null);
    this.isSubmitting.set(true);

    const { code } = this.codeForm.getRawValue();
    const email = this.authService.user()?.email;
    if (!email) {
      this.isSubmitting.set(false);
      this.errorMessage.set('Не вдалося отримати електронну пошту користувача');
      return;
    }

    // verifyEmail() у AuthService сам чистить сесію (tap -> clearSession) при успіху
    this.authService.verifyEmail(email, code).subscribe({
      next: () => {
        this.dialogRef.close(true);
        this.router.navigateByUrl('/login');
      },
      error: () => {
        this.isSubmitting.set(false);
        this.errorMessage.set('Невірний або прострочений код');
      },
    });
  }

  backToForm(): void {
    this.step.set('form');
    this.errorMessage.set(null);
  }
}