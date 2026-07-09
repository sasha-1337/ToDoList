import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { CategoryDto, CreateCategoryRequest, UpdateCategoryRequest } from '../models/category.models';

const BASE_URL = '/api/Categories';

@Injectable({ providedIn: 'root' })
export class CategoryService {
  private readonly http = inject(HttpClient);

  getAll(): Observable<CategoryDto[]> {
    return this.http.get<CategoryDto[]>(BASE_URL);
  }

  create(name: string): Observable<CategoryDto> {
    return this.http.post<CategoryDto>(BASE_URL, { name } satisfies CreateCategoryRequest);
  }

  update(id: string, name: string): Observable<CategoryDto> {
    return this.http.put<CategoryDto>(`${BASE_URL}/${id}`, { name } satisfies UpdateCategoryRequest);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${BASE_URL}/${id}`);
  }
}