using ToDoApp.DTOs.ToDoDTOs;

namespace ToDoApp.Services
{
    public interface ITaskItemService
    {
        Task<PagedResponseDto<TaskItemResponseDto>> GetAsync(int userId, string? cursor, 
            int pageSize, int? categoryId, string? searchQuery, string? sortDirection, bool? sortIsCompleted);
        Task<TaskItemResponseDto?> GetByIdAsync(int taskId, int userId);
        Task<TaskItemResponseDto> CreateAsync(TaskItemCreateUpdateDto taskDto, int userId);
        Task<bool> UpdateAsync(int taskId, TaskItemCreateUpdateDto dto, int userId);
        Task<bool> DeleteAsync(int taskId, int userId);
        Task<UpdateTaskStatusResponseDto> ToggleStatusAsync(int taskId, int userId);
        Task BulkMoveAsync(List<int> taskIds, int? categoryId, int userId);
        Task BulkDeleteAsync(List<int> taskIds, int userId);
        Task<BulkUpdateStatusResponseDto> BulkUpdateStatusAsync(List<int> taskIds, bool isCompleted, int userId);
    }
}
