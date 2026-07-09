import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, tap, of } from 'rxjs';

import {
  BulkMoveRequest,
  BulkDeleteRequest,
  BulkDeleteResponse,
    BulkSetCompletedRequest as BulkUpdateStatusRequest,
  CreateTaskRequest,
  PagedResult,
  TaskDto,
  TaskQueryParams,
  UpdateTaskRequest,
  UpdateTaskStatusRequest,
  UpdateTaskStatusResponse,
  BulkUpdateStatusResponse,
} from '../models/task.models';
import { UserScoreStore } from '../state/user-score.store';

const BASE_URL = '/api/TaskItems';
const DEFAULT_PAGE_SIZE = 15;

@Injectable({ providedIn: 'root' })
export class TaskService {
  private readonly http = inject(HttpClient);
  private readonly scoreStore = inject(UserScoreStore);

  /** Курсорна пагінація: передаємо nextCursor з попередньої відповіді, поки hasMore === true. */
  getPaged(params: TaskQueryParams): Observable<PagedResult<TaskDto>> {
    let httpParams = new HttpParams().set('pageSize', String(DEFAULT_PAGE_SIZE));

    if (params.categoryId) {
      httpParams = httpParams.set('categoryId', params.categoryId);
    }
    if (params.searchQuery) {
      httpParams = httpParams.set('searchQuery', params.searchQuery);
    }
    if (params.cursor) {
      httpParams = httpParams.set('cursor', params.cursor);
    }
    if (params.sortDirection) {
      httpParams = httpParams.set('sortDirection', params.sortDirection);
    }
    if (params.isCompleted !== undefined && params.isCompleted !== null) {
      httpParams = httpParams.set('isCompleted', String(params.isCompleted));
    }

    return this.http.get<PagedResult<TaskDto>>(BASE_URL, { params: httpParams });
  }

  create(request: CreateTaskRequest): Observable<TaskDto> {
    return this.http.post<TaskDto>(BASE_URL, request);
  }

  update(id: string, request: UpdateTaskRequest): Observable<TaskDto> {
    return this.http.put<TaskDto>(`${BASE_URL}/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${BASE_URL}/${id}`);
  }

  /**
   * Зміна статусу виконання. Бекенд перераховує TotalScore користувача
   * і повертає його разом з оновленою таскою — записуємо в UserScoreStore,
   * щоб рахунок в UI (напр. в сайдбарі) оновився без окремого запиту профілю.
   */
  setCompleted(id: string, isCompleted: boolean): Observable<UpdateTaskStatusResponse> {
    return this.http
      .patch<UpdateTaskStatusResponse>(`${BASE_URL}/${id}/status`, {
        isCompleted,
      } satisfies UpdateTaskStatusRequest)
      .pipe(tap((res) => {this.scoreStore.set(res.totalScore); console.log("Total Score:", res.totalScore)}));

  }

   // ============================================================
  // =====================  МАСОВІ ДІЇ (BULK)  ====================
  // ============================================================

  bulkMove(taskIds: string[], categoryId: string | null): Observable<void> {
    if (taskIds.length === 0) return of(void 0);
    return this.http.post<void>(`${BASE_URL}/bulk-move`, {
      taskIds,
      categoryId,
    } satisfies BulkMoveRequest);
  }
 
  bulkDelete(taskIds: string[]): Observable<void> {
    if (taskIds.length === 0) return of(void 0);
    return this.http.post<void>(`${BASE_URL}/bulk-delete`, {
      taskIds, } satisfies BulkDeleteRequest);
  };
 

  bulkSetCompleted(taskIds: string[], isCompleted: boolean): Observable<BulkUpdateStatusResponse> {
    if (taskIds.length === 0) return of({totalScore: 0, updatedTasks: []} as BulkUpdateStatusResponse);
    return this.http.patch<BulkUpdateStatusResponse>(`${BASE_URL}/bulk-status`, {
      taskIds,
      isCompleted,
    } satisfies BulkUpdateStatusRequest).pipe(
      tap((res) => this.scoreStore.set(res.totalScore) 
      ),
    );
  }
}