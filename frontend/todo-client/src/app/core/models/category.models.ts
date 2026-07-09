export interface CategoryDto {
  id: string;
  name: string;
  /** Кількість тасок у категорії — зручно показати бейджем у сайдбарі, якщо бекенд це рахує */
  taskCount?: number;
}

export interface CreateCategoryRequest {
  name: string;
}

export interface UpdateCategoryRequest {
  name: string;
}