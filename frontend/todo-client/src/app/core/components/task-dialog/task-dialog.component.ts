import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatSelectModule } from '@angular/material/select';

import { TaskDto  } from '../../models/task.models';
import { CategoryDto } from '../../models/category.models';
import { CategoryService } from '../../services/category.service';

export interface TaskDialogData {
  categoryId: string | null;
  task?: TaskDto;
  excludeCategoryId?: string | null; // виключити категорію до якої належить таска
}

export interface TaskDialogResult {
  title: string;
  description: string;
  deadline: string | null;
  score: number;
  categoryId: string;
}

@Component({
  selector: 'app-task-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatDatepickerModule,
    MatSelectModule,
  ],
  templateUrl: './task-dialog.component.html',
})
export class TaskDialogComponent {
  readonly dialogRef = inject(MatDialogRef<TaskDialogComponent, TaskDialogResult>);
  private readonly data = inject<TaskDialogData>(MAT_DIALOG_DATA);
  private readonly fb = inject(FormBuilder);
  protected categoryService = inject(CategoryService);

  readonly categories = signal<CategoryDto[]>([]);

  readonly isEditMode = !!this.data.task;
  readonly isLoading = signal(true);

  readonly form = this.fb.group({
    title: [this.data.task?.title ?? '', [Validators.required, Validators.maxLength(100)]],
    description: [this.data.task?.description ?? '', [Validators.maxLength(1000)]],
    deadline: [this.data?.task?.deadline ? new Date(this.data.task.deadline) : null ],
    categoryId: [this.data?.task?.categoryId ? this.data.categoryId : null],
  });

  constructor() {
    this.categoryService.getAll().subscribe({
      next: (categories) => {
        this.categories.set(
          this.data.excludeCategoryId
            ? categories.filter((c) => c.id !== this.data.excludeCategoryId)
            : categories,
        );
      },
      complete: () => this.isLoading.set(false),
    });
  }

  onSubmit(): void {
    if (this.form.invalid) return;
    const raw = this.form.getRawValue();

    this.dialogRef.close({
      title: raw.title?.trim() ?? '',
      description: raw.description?.trim() ?? '',
      deadline: raw.deadline ? (raw.deadline as Date).toISOString() : null,
      categoryId: raw.categoryId ?? null,
      score: this.data.task?.score ?? 0,
    });
  }
}