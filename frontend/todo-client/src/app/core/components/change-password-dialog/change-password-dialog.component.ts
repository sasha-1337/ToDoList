import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { HttpErrorResponse } from '@angular/common/http';

import { AuthService } from '../../auth/services/auth.service';

function passwordsMatchValidator(control: AbstractControl): ValidationErrors | null {
  const password = control.get('newPassword')?.value;
  const confirm = control.get('confirmNewPassword')?.value;
  return password && confirm && password !== confirm ? { passwordsMismatch: true } : null;
}

type Step = 'form' | 'verify';

@Component({
  selector: 'app-change-password-dialog',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule],
  templateUrl: './change-password-dialog.component.html',
})
export class ChangePasswordDialogComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  readonly dialogRef = inject(MatDialogRef<ChangePasswordDialogComponent, boolean>);

  readonly step = signal<Step>('form');
  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly passwordForm = this.fb.nonNullable.group(
    {
      oldPassword: ['', [Validators.required]],
      newPassword: ['', [Validators.required, Validators.minLength(8)]],
      confirmNewPassword: ['', [Validators.required]],
    },
    { validators: passwordsMatchValidator },
  );

  readonly codeForm = this.fb.nonNullable.group({
    code: ['', [Validators.required, Validators.minLength(4)]],
  });

  onRequestSubmit(): void {
    if (this.passwordForm.invalid || this.isSubmitting()) return;

    this.errorMessage.set(null);
    this.isSubmitting.set(true);

    const { oldPassword, newPassword } = this.passwordForm.getRawValue();
    this.authService.requestChangePassword(oldPassword, newPassword).subscribe({
      next: () => {
        this.step.set('verify');
        this.isSubmitting.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.isSubmitting.set(false);
        this.errorMessage.set(err.status === 401 ? 'Старий пароль невірний' : 'Не вдалось надіслати код');
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

    this.authService.verifyEmail(email, code).subscribe({
      next: () => this.dialogRef.close(true),
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