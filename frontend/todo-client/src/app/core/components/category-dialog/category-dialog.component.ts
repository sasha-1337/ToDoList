import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';

export interface CategoryDialogData {
  /** Якщо передано — режим редагування, інакше створення нової категорії */
  category?: { id: string; name: string };
}

export interface CategoryDialogResult {
  name: string;
}

@Component({
  selector: 'app-category-dialog',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule],
  template: `
    <h2 mat-dialog-title>{{ isEditMode ? 'Редагувати категорію' : 'Нова категорія' }}</h2>

    <form [formGroup]="form" (ngSubmit)="onSubmit()">
      <div mat-dialog-content>
        <mat-form-field appearance="outline" class="w-full">
          <mat-label>Назва категорії</mat-label>
          <input matInput formControlName="name" maxlength="50" cdkFocusInitial />
          @if (form.controls.name.hasError('required')) {
            <mat-error>Назва обов'язкова</mat-error>
          }
        </mat-form-field>
      </div>

      <div mat-dialog-actions align="end">
        <button mat-button type="button" (click)="dialogRef.close()">Скасувати</button>
        <button mat-flat-button color="primary" type="submit" [disabled]="form.invalid">
          {{ isEditMode ? 'Зберегти' : 'Створити' }}
        </button>
      </div>
    </form>
  `,
})
export class CategoryDialogComponent {
  readonly dialogRef = inject(MatDialogRef<CategoryDialogComponent, CategoryDialogResult>);
  private readonly data = inject<CategoryDialogData>(MAT_DIALOG_DATA, { optional: true }) ?? {};
  private readonly fb = inject(FormBuilder);

  readonly isEditMode = !!this.data.category;

  readonly form = this.fb.nonNullable.group({
    name: [this.data.category?.name ?? '', [Validators.required, Validators.maxLength(50)]],
  });

  onSubmit(): void {
    if (this.form.invalid) return;
    this.dialogRef.close({ name: this.form.controls.name.value.trim() });
  }
}