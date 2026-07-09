using Microsoft.EntityFrameworkCore;
using ToDoApp.Data;
using ToDoApp.Models;
using ToDoApp.DTOs.ToDoDTOs;
using Org.BouncyCastle.Tls;

namespace ToDoApp.Services
{
    public class TaskItemService : ITaskItemService
    {
        private readonly ToDoAppDbContext _dbContext;
        private readonly ILogger<TaskItemService> _logger;
        private readonly IAiScoringService _aiScoringService;

        public TaskItemService(ToDoAppDbContext dbContext, ILogger<TaskItemService> logger, IAiScoringService aiScoringService)
        {
            _dbContext = dbContext;
            _logger = logger;
            _aiScoringService = aiScoringService;
        }

        public async Task<PagedResponseDto<TaskItemResponseDto>> GetAsync(int userId, string? cursor, 
                                                                                int pageSize, int? categoryId, 
                                                                                string? searchQuery, string? sortDirection = "desc", 
                                                                                bool? sortIsCompleted = null)
        {
            try
            {
                var query = _dbContext.TaskItems
                    .Include(t => t.Category)
                    .Where(t => t.UserId == userId)
                    .AsQueryable();

                if (categoryId.HasValue)
                {
                    query = query.Where(t => t.CategoryId == categoryId.Value);
                }

                if (!string.IsNullOrEmpty(searchQuery))
                {
                    var lowerSearchQuery = searchQuery.ToLower();
                    query = query.Where(t => t.Title.Contains(lowerSearchQuery)
                                          || (t.Description != null && t.Description.Contains(lowerSearchQuery)));
                }

                if (sortIsCompleted.HasValue)
                {
                    query = query.Where(t => t.IsCompleted == sortIsCompleted.Value);
                }

                bool isDescending = sortDirection?.ToLower() != "asc";

                if (!string.IsNullOrEmpty(cursor) && DateTime.TryParse(cursor, out var cursorDate))
                {
                    if (isDescending)
                        query = query.Where(t => t.CreatedAt < cursorDate);
                    else
                        query = query.Where(t => t.CreatedAt > cursorDate);
                }

                if (isDescending)
                    query = query.OrderByDescending(t => t.CreatedAt);
                else
                    query = query.OrderBy(t => t.CreatedAt);

                var items = await query
                    .Take(pageSize+1)
                    .Select(t => new TaskItemResponseDto
                    {
                        Id = t.Id,
                        Title = t.Title,
                        Description = t.Description,
                        Score = t.Score,
                        IsCompleted = t.IsCompleted,
                        CreatedAt = t.CreatedAt,
                        CategoryId = t.CategoryId,
                        CategoryName = t.Category != null ? t.Category.Name : null
                    })
                    .ToListAsync();

                bool hasMore = items.Count > pageSize;
                if (hasMore)
                    items.RemoveAt(pageSize); 
                
                string? nextCursor = items.Any() ? items.Last().CreatedAt.ToString("O") : null;

                return new PagedResponseDto<TaskItemResponseDto>
                {
                    Items = items,
                    NextCursor = nextCursor,
                    HasMore = hasMore,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving tasks for user {UserId}.", userId);
                throw;
            }
        }

        public async Task<TaskItemResponseDto?> GetByIdAsync(int taskId, int userId)
        {
            try
            {
                var task = await _dbContext.TaskItems
                    .Include(t => t.Category)
                    .FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);
                if (task == null)
                {
                    _logger.LogWarning("Task with ID {TaskId} not found for user {UserId}.", taskId, userId);
                    return null;
                }
                return new TaskItemResponseDto
                {
                    Id = task.Id,
                    Title = task.Title,
                    Description = task.Description,
                    IsCompleted = task.IsCompleted,
                    CreatedAt = task.CreatedAt,
                    CategoryId = task.CategoryId,
                    CategoryName = task.Category != null ? task.Category.Name : null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving task {TaskId} for user {UserId}.", taskId, userId);
                throw;
            }

        }
        public async Task<TaskItemResponseDto> CreateAsync(TaskItemCreateUpdateDto taskDto, int userId)
        {
            try
            {
                int estimatedScore = await _aiScoringService.EstimateTaskComplexityAsync(taskDto.Title, taskDto.Description);

                var task = new TaskItem
                {
                    Title = taskDto.Title,
                    Description = taskDto.Description,
                    IsCompleted = taskDto.IsCompleted,
                    CreatedAt = DateTime.UtcNow,
                    UserId = userId,
                    CategoryId = taskDto.CategoryId,
                    Score = estimatedScore,
                    Deadline = taskDto.Deadline
                };
                await _dbContext.TaskItems.AddAsync(task);
                await _dbContext.SaveChangesAsync();

                if (task.CategoryId.HasValue)
                {
                    await _dbContext.Entry(task).Reference(t => t.Category).LoadAsync();
                }

                _logger.LogInformation("Task with ID {TaskId} created for user {UserId}.", task.Id, userId);

                return new TaskItemResponseDto
                {
                    Id = task.Id,
                    Title = task.Title,
                    Description = task.Description,
                    IsCompleted = task.IsCompleted,
                    CreatedAt = task.CreatedAt,
                    CategoryId = task.CategoryId,
                    CategoryName = task.Category?.Name,
                    Score = task.Score,
                    Deadline= task.Deadline
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating a new task for user {UserId}.", userId);
                throw;
            }
        }

        public async Task<bool> UpdateAsync(int taskId, TaskItemCreateUpdateDto dto, int userId)
        {
            try
            {
                var task = await _dbContext.TaskItems.Include(t => t.User).FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);
                if (task == null)
                {
                    _logger.LogWarning("Task with ID {TaskId} not found for user {UserId}.", taskId, userId);
                    return false;
                }

                if (task.IsCompleted)
                {
                    task.User.TotalScore -= task.AwardedScore;
                    task.AwardedScore = 0;
                }

                if (task.Title != dto.Title || task.Description != dto.Description)
                    task.Score = await _aiScoringService.EstimateTaskComplexityAsync(dto.Title, dto.Description);
                
                task.Title = dto.Title;
                task.Description = dto.Description;
                task.IsCompleted = dto.IsCompleted;
                task.CategoryId = dto.CategoryId;
                task.Deadline = dto.Deadline;

                if (task.IsCompleted)
                {
                    task.AwardedScore = CalculateReward(task);
                    task.User.TotalScore += task.AwardedScore;
                }

                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Task with ID {TaskId} updated for user {UserId}.", task.Id, userId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating task {TaskId} for user {UserId}.", taskId, userId);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int taskId, int userId)
        {
            try
            {
                var task = await _dbContext.TaskItems.FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);
                if (task == null)
                {
                    _logger.LogWarning("Task with ID {TaskId} not found for user {UserId}.", taskId, userId);
                    return false;
                }
                _dbContext.TaskItems.Remove(task);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Task with ID {TaskId} deleted for user {UserId}.", task.Id, userId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while deleting task {TaskId} for user {UserId}.", taskId, userId);
                throw;
            }
        }

        public async Task<UpdateTaskStatusResponseDto> ToggleStatusAsync(int taskId, int userId)
        {
            var task = await _dbContext.TaskItems
                            .Include(t => t.User)
                            .FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);

            if (task == null) return null;

            task.IsCompleted = !task.IsCompleted;
            GiveRewardToUser(task);

            await _dbContext.SaveChangesAsync();

            return new UpdateTaskStatusResponseDto 
            { 
                IsCompleted = task.IsCompleted, 
                TotalScore = task.User.TotalScore 
            };
        }

        public async Task BulkMoveAsync(List<int> taskIds, int? categoryId, int userId)
        {
            await _dbContext.TaskItems
                .Where(t => taskIds.Contains(t.Id) && t.UserId == userId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.CategoryId, categoryId));
        }

        public async Task BulkDeleteAsync(List<int> taskIds, int userId)
        {
            await _dbContext.TaskItems
                .Where(t => taskIds.Contains(t.Id) && t.UserId == userId)
                .ExecuteDeleteAsync();
        }

        public async Task<BulkUpdateStatusResponseDto> BulkUpdateStatusAsync(List<int> taskIds, bool isCompleted, int userId)
        {
            var tasks = await _dbContext.TaskItems
                .Include(t => t.User)
                .Where(t => taskIds.Contains(t.Id) && t.UserId == userId && t.IsCompleted != isCompleted)
                .ToListAsync();

            if (!tasks.Any())
            {
                var currentUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
                return new BulkUpdateStatusResponseDto
                {
                    TotalScore = currentUser?.TotalScore ?? 0
                };
            }

            var user = tasks.First().User;
            foreach (var task in tasks)
            {
                task.IsCompleted = isCompleted;
                GiveRewardToUser(task);
            }

            await _dbContext.SaveChangesAsync();

            var updatedTaskDtos = tasks.Select(t => new TaskItemResponseDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                Score = t.Score,
                IsCompleted = t.IsCompleted,
                CreatedAt = t.CreatedAt,
                CategoryId = t.CategoryId,
                CategoryName = t.Category?.Name,
                Deadline = t.Deadline
            }).ToList();

            return new BulkUpdateStatusResponseDto
            {
                TotalScore = user.TotalScore,
                UpdatedTasks = updatedTaskDtos
            };
        }

        private int CalculateReward(TaskItem task)
        {
            if (task.Score <= 0) return 0;

            if (!task.Deadline.HasValue || DateTime.UtcNow <= task.Deadline.Value)
                return task.Score;

            return task.Score / 2;
        }

        private void GiveRewardToUser(TaskItem task)
        {
            if (task.IsCompleted)
            {
                task.AwardedScore = CalculateReward(task);
                task.User.TotalScore += task.AwardedScore;
            }
            else
            {
                task.User.TotalScore -= task.AwardedScore;
                task.AwardedScore = 0;
            }
        }
    }
}