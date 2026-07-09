import { Component, Input, OnChanges, SimpleChanges, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog } from '@angular/material/dialog';
import { FormsModule } from '@angular/forms';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';

import { TaskService } from '../../services/task.service';
import { TaskDto } from '../../models/task.models';
import { TaskDialogComponent, TaskDialogResult } from '../task-dialog/task-dialog.component';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { debounceTime, distinctUntilChanged, Subject } from 'rxjs';
import { MoveTasksDialogComponent, MoveTasksDialogData, MoveTasksDialogResult } from '../move-task-dialog/move-tasks-dialog.component';

const PAGE_SIZE = 10;
/** За скільки пікселів до низу контейнера підвантажувати наступну сторінку */
const SCROLL_THRESHOLD_PX = 100;

@Component({
  selector: 'app-task-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatCheckboxModule,
    MatIconModule,
    MatButtonModule,
    MatMenuModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    MatInputModule,
    MatFormFieldModule,
    
  ],
  templateUrl: './task-list.component.html',
})
export class TaskListComponent implements OnChanges {
  private readonly taskService = inject(TaskService);
  private readonly dialog = inject(MatDialog);
  

  /** null = "Усі таски" (без фільтра за категорією) */
  @Input() categoryId: string | null = null;


  readonly tasks = signal<TaskDto[]>([]);
  readonly loading = signal(false);
  readonly hasMore = signal(true);
  readonly initialLoadDone = signal(false);

  readonly searchTerm = signal('');
  private readonly search$ = new Subject<string>();

 // ============================================================
 // ==================  МНОЖИННИЙ ВИБІР ТАСОК  ===================
 // ============================================================
 
  readonly isSelectionMode = signal(false);
  readonly selectedIds = signal<Set<string>>(new Set());
  readonly selectedCount = computed(() => this.selectedIds().size);
  readonly isBulkActionRunning = signal(false);

  constructor() {
    // Підписка на зміну пошуку з затримкою
    this.search$
      .pipe(debounceTime(1000), distinctUntilChanged(), takeUntilDestroyed())
      .subscribe((term) => {
        this.searchTerm.set(term);
        this.reload(); // Перезавантажуємо список з новим параметром
      });
  }

  /** Сортування тасок за датою */
  readonly sortDirection = signal<'asc' | 'desc'>('desc');
  toggleSort(): void {
    this.sortDirection.update((current) => current === 'desc' ? 'asc' : 'desc');
    this.reload(); // Перезавантажуємо список після зміни сортування
  }

  /** Фільтри для запиту списку тасок */
  readonly filterStatus = signal<'all' | 'active' | 'completed'>('all');
  setFilter(status: 'all' | 'active' | 'completed'): void {
    this.filterStatus.set(status);
    this.reload();
  }

  private nextCursor: string | null = null;

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['categoryId']) {
      this.reload();
    }
  }

  onSearchInput(value: string): void {
      this.search$.next(value.trim());
  }

  /** Скидає список і вантажить першу сторінку — викликається при зміні категорії/пошуку. */
  reload(): void {
    this.tasks.set([]);
    this.nextCursor = null;
    this.hasMore.set(true);
    this.initialLoadDone.set(false);
    this.loadMore();
  }

  loadMore(): void {
    if (this.loading() || !this.hasMore()) return;

    this.loading.set(true);

    let taskCompletedFilter: boolean | null = null;
    if (this.filterStatus() === 'active') taskCompletedFilter = false;
    if (this.filterStatus() === 'completed') taskCompletedFilter = true;
    
    this.taskService
      .getPaged({
        categoryId: this.categoryId,
        searchQuery: this.searchTerm() || null,
        cursor: this.nextCursor,
        pageSize: PAGE_SIZE,
        sortDirection: this.sortDirection(),
        isCompleted: taskCompletedFilter,
      })
      .subscribe({
        next: (res) => {
          this.tasks.update((list) => [...list, ...res.items]);
          this.nextCursor = res.nextCursor;
          this.hasMore.set(res.hasMore);
        },
        error: () => this.loading.set(false),
        complete: () => {
          this.loading.set(false);
          this.initialLoadDone.set(true);
        },
      });
  }

  /** Обробник скролу контейнера — тригерить loadMore() біля нижньої межі списку. */
  onScroll(event: Event): void {
    const el = event.target as HTMLElement;
    const distanceToBottom = el.scrollHeight - el.scrollTop - el.clientHeight;
    if (distanceToBottom < SCROLL_THRESHOLD_PX) {
      
      this.loadMore();
    }
  }

  toggleStatus(task: TaskDto): void {
    const previous = task.isCompleted;
    // оптимістичне оновлення — одразу міняємо чекбокс, відкатуємо при помилці
    this.patchTaskInList(task.id, { isCompleted: !previous });

    this.taskService.setCompleted(task.id, !previous).subscribe({
      next: (res) => this.patchTaskInList(task.id, res.task),
      error: () => this.patchTaskInList(task.id, { isCompleted: previous }),
    });
  }

  openCreateDialog(): void {

    const ref = this.dialog.open<TaskDialogComponent, unknown, TaskDialogResult>(TaskDialogComponent, {
      width: '480px',
      data: { categoryId: this.categoryId },
    });

    ref.afterClosed().subscribe((result) => {
      if (!result) return;
      this.taskService
        .create(result)
        .subscribe((created) => {
          if (created && created.id) {
            if (this.categoryId !== null && created.categoryId !== this.categoryId) return;
            this.tasks.update((list) => [created, ...list]);
            this.reload();
          } else {
            this.reload();
          }
        });
    });
  }

  openEditDialog(task: TaskDto): void {
    const ref = this.dialog.open<TaskDialogComponent, unknown, TaskDialogResult>(TaskDialogComponent, {
      width: '480px',
      data: { categoryId: task.categoryId, task },
    });

    ref.afterClosed().subscribe((result) => {
      if (!result) return;
      this.taskService
        .update(task.id, result)
        .subscribe((updated) => this.patchTaskInList(task.id, updated));
    });
  }

  deleteTask(task: TaskDto): void {
    if (!confirm(`Видалити таску "${task.title}"?`)) return;
    this.taskService.delete(task.id).subscribe(() => {
      this.tasks.update((list) => list.filter((t) => t.id !== task.id));
    });
  }

  private patchTaskInList(id: string, patch: Partial<TaskDto>): void {
    this.tasks.update((list) => list.map((t) => (t.id === id ? { ...t, ...patch } : t)));
    this.reload();
  }


  // ============================================================
  // ==================  МНОЖИННИЙ ВИБІР: ДІЇ  ===================
  // ============================================================
 
  toggleSelectionMode(): void {
    this.isSelectionMode.update((v) => !v);
    if (!this.isSelectionMode()) {
      this.clearSelection();
    }
  }
 
  exitSelectionMode(): void {
    this.isSelectionMode.set(false);
    this.clearSelection();
  }
 
  clearSelection(): void {
    this.selectedIds.set(new Set());
  }
 
  isSelected(id: string): boolean {
    return this.selectedIds().has(id);
  }
 
  toggleSelected(task: TaskDto): void {
    this.selectedIds.update((set) => {
      const next = new Set(set);
      if (next.has(task.id)) {
        next.delete(task.id);
      } else {
        next.add(task.id);
      }
      return next;
    });
  }
 
  /** Виділити/зняти виділення з усіх тасок, що зараз завантажені у списку */
  toggleSelectAllLoaded(): void {
    const allLoadedIds = this.tasks().map((t) => t.id);
    const allAlreadySelected = allLoadedIds.length > 0 && allLoadedIds.every((id) => this.isSelected(id));
    this.selectedIds.set(allAlreadySelected ? new Set() : new Set(allLoadedIds));
  }
 
  bulkDeleteSelected(): void {
    const ids = Array.from(this.selectedIds());
    if (ids.length === 0 || this.isBulkActionRunning()) return;
    if (!confirm(`Видалити ${ids.length} обраних тасок? Це не можна скасувати.`)) return;
 
    this.isBulkActionRunning.set(true);
    this.taskService.bulkDelete(ids).subscribe({
      next: () => {
        this.tasks.update((list) => list.filter((t) => !this.selectedIds().has(t.id)));
        this.clearSelection();
      },
      complete: () => this.isBulkActionRunning.set(false),
      error: () => this.isBulkActionRunning.set(false),
    });
  }
 
  bulkMarkCompleted(): void {
    const ids = Array.from(this.selectedIds());
    if (ids.length === 0 || this.isBulkActionRunning()) return;
 
    this.isBulkActionRunning.set(true);
    this.taskService.bulkSetCompleted(ids, true).subscribe({
      next: (res) => {
        res.updatedTasks.forEach((updatedTask) => this.patchTaskInList(updatedTask.id, updatedTask));
        this.clearSelection();
      },
      complete: () => this.isBulkActionRunning.set(false),
      error: () => this.isBulkActionRunning.set(false),
    });
  }
 
  openBulkMoveDialog(): void {
    const ids = Array.from(this.selectedIds());
    if (ids.length === 0 || this.isBulkActionRunning()) return;
 
    const ref = this.dialog.open<MoveTasksDialogComponent, MoveTasksDialogData, MoveTasksDialogResult>(
      MoveTasksDialogComponent,
      {
        width: '360px',
        data: { taskCount: ids.length, excludeCategoryId: this.categoryId },
      },
    );
 
    ref.afterClosed().subscribe((result) => {
      if (!result) return;
 
      this.isBulkActionRunning.set(true);
      this.taskService.bulkMove(ids, result.categoryId).subscribe({
        next: () => {
          // Якщо зараз відкрита конкретна категорія і перенесли в іншу — таски зникають зі списку.
          // Якщо перегляд "Усі таски" (categoryId === null) — просто оновлюємо їхній categoryId на місці.
          if (this.categoryId) {
            this.tasks.update((list) => list.filter((t) => !this.selectedIds().has(t.id)));
          } else {
            this.tasks.update((list) =>
              list.map((t) => (this.selectedIds().has(t.id) ? { ...t, categoryId: result.categoryId ?? '' } : t)),
            );
          }
          this.clearSelection();
        },
        complete: () => this.isBulkActionRunning.set(false),
        error: () => this.isBulkActionRunning.set(false),
      });
    });
  }
}