import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';

import { SidebarComponent } from '../sidebar/sidebar.component';
import { TaskListComponent } from '../task-list/task-list.component';

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [CommonModule, SidebarComponent, TaskListComponent],
  templateUrl: './layout.component.html',
})
export class LayoutComponent {
  /** null = не вибрано жодної категорії -> показуємо всі таски */
  readonly selectedCategoryId = signal<string | null>(null);

  onCategorySelected(id: string | null): void {
    this.selectedCategoryId.set(id);
  }
}