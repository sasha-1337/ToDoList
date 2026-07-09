export interface TaskDto {
  id: string;
  categoryId: string;
  title: string;
  description: string;
  /** ISO 8601 рядок дати, напр. 2026-07-10T18:00:00Z */
  deadline: string | null;
  score: number;
  isCompleted: boolean;
  createdAt: string;
  categoryName: string | null;
}

export interface CreateTaskRequest {
  categoryId: string | null;
  title: string;
  description: string;
  deadline: string | null;
  score: number;
}

export interface UpdateTaskRequest {
  title: string;
  description: string;
  deadline: string | null;
  score: number;
  categoryId: string;
}

export interface UpdateTaskStatusRequest {
  isCompleted: boolean;
}

/**
 * Бекенд при зміні статусу оновлює TotalScore користувача,
 * тому повертає і оновлену таску, і актуальний загальний рахунок —
 * щоб фронту не робити ще один запит за профілем.
 */
export interface UpdateTaskStatusResponse {
  task: TaskDto;
  totalScore: number;
}

/** Параметри запиту списку тасок з курсорною пагінацією + фільтрами */
export interface TaskQueryParams {
  categoryId?: string | null;
  searchQuery?: string | null;
  /** null/undefined — перша сторінка */
  cursor?: string | null;
  pageSize?: number;
  sortDirection?: 'asc' | 'desc'; 
  isCompleted?: boolean | null;
}

/** Уніфікована форма відповіді для курсорної пагінації */
export interface PagedResult<T> {
  items: T[];
  nextCursor: string | null;
  hasMore: boolean;
}

// === Масові дії над вибраними тасками ===
 
/**
 * DTO для POST /api/TaskItems/bulk-move.
 * null у categoryId — "прибрати з усіх категорій" (перенести в "Без категорії").
 */
export interface BulkMoveRequest {
  taskIds: string[];
  categoryId: string | null;
}

export interface BulkDeleteRequest {
  taskIds: string[];
}

export interface BulkDeleteResponse {
  taskIds: string[];
  totalScore: string | null;
}

export interface BulkSetCompletedRequest {
  taskIds: string[];
  isCompleted: boolean;
}
export interface BulkUpdateStatusResponse {
  totalScore: number;
  updatedTasks: TaskDto[];
}