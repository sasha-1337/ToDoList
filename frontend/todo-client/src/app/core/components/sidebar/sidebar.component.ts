import { Component, EventEmitter, Output, effect, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatTooltipModule } from '@angular/material/tooltip';

import { CategoryService } from '../../services/category.service';
import { CategoryDto } from '../../models/category.models';
import { AuthService } from '../../auth/services/auth.service';
import { UserMenuComponent } from '../user-menu/user-menu.component';
import { UserScoreStore } from '../../state/user-score.store';

import { CategoryDialogComponent, CategoryDialogResult } from '../category-dialog/category-dialog.component';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    MatButtonModule,
    MatTooltipModule,
    UserMenuComponent,
  ],
  templateUrl: './sidebar.component.html',
})
export class SidebarComponent {
  private readonly categoryService = inject(CategoryService);
  private readonly dialog = inject(MatDialog);
  protected readonly authService = inject(AuthService);
  protected readonly scoreStore = inject(UserScoreStore);

  /** Емітиться при виборі категорії; null означає "Усі таски" */
  @Output() categorySelected = new EventEmitter<string | null>();

  readonly categories = signal<CategoryDto[]>([]);
  readonly activeCategoryId = signal<string | null>(null);
  readonly isLoading = signal(false);


  constructor() {
    
    this.loadCategories();
  }

  loadCategories(): void {
    this.isLoading.set(true);
    this.categoryService.getAll().subscribe({
      next: (categories) => this.categories.set(categories),
      error: () => this.isLoading.set(false),
      complete: () => this.isLoading.set(false),
    });
  }

  selectCategory(id: string | null): void {
    this.activeCategoryId.set(id);
    this.categorySelected.emit(id);
  }

  openCreateDialog(): void {
    const ref = this.dialog.open<CategoryDialogComponent, unknown, CategoryDialogResult>(CategoryDialogComponent, {
      width: '400px',
    });

    ref.afterClosed().subscribe((result) => {
      if (!result) return;
      this.categoryService.create(result.name).subscribe((created) => {
        this.categories.update((list) => [...list, created]);
      });
    });
  }

  openEditDialog(category: CategoryDto, event: Event): void {
    event.stopPropagation(); // не активувати вибір категорії при кліку на іконку
    const ref = this.dialog.open<CategoryDialogComponent, unknown, CategoryDialogResult>(CategoryDialogComponent, {
      width: '400px',
      data: { category },
    });

    ref.afterClosed().subscribe((result) => {
      if (!result) return;
      this.categoryService.update(category.id, result.name).subscribe((updated) => {
        this.categories.update((list) => list.map((c) => (c.id === updated.id ? updated : c)));
      });
    });
  }

  deleteCategory(category: CategoryDto, event: Event): void {
    event.stopPropagation();
    if (!confirm(`Видалити категорію "${category.name}"? Це не можна скасувати.`)) return;

    this.categoryService.delete(category.id).subscribe(() => {
      this.categories.update((list) => list.filter((c) => c.id !== category.id));
      if (this.activeCategoryId() === category.id) {
        this.selectCategory(null);
      }
    });
  }
}