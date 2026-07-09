import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatRadioModule } from '@angular/material/radio';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { CategoryService } from '../../services/category.service';
import { CategoryDto } from '../../models/category.models';

export interface MoveTasksDialogData {
  taskCount: number;
  excludeCategoryId?: string | null;
}

export interface MoveTasksDialogResult {
  categoryId: string | null;
}

@Component({
  selector: 'app-move-tasks-dialog',
  standalone: true,
  imports: [CommonModule, FormsModule, MatDialogModule, MatButtonModule, MatRadioModule, MatProgressSpinnerModule],
  templateUrl: './move-tasks-dialog.component.html',
})
export class MoveTasksDialogComponent {
  private readonly categoryService = inject(CategoryService);
  readonly dialogRef = inject(MatDialogRef<MoveTasksDialogComponent, MoveTasksDialogResult>);
  readonly data = inject<MoveTasksDialogData>(MAT_DIALOG_DATA);

  readonly categories = signal<CategoryDto[]>([]);
  readonly isLoading = signal(true);
  selectedCategoryId: string | null = null;

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

  confirm(): void {
    this.dialogRef.close({ categoryId: this.selectedCategoryId });
  }
}