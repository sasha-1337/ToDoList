namespace ToDoApp.DTOs.ToDoDTOs
{
    public class BulkMoveRequestDto
    {
        public List<int> TaskIds { get; set; } = new();
        public int? CategoryId { get; set; } // null, якщо масово робимо їх "Без категорії"
    }

    public class BulkDeleteRequestDto
    {
        public List<int> TaskIds { get; set; } = new();
    }

    public class BulkUpdateStatusRequestDto
    {
        public List<int> TaskIds { get; set; } = new();
        public bool IsCompleted { get; set; }
    }

    public class BulkUpdateStatusResponseDto
    {
        public int TotalScore { get; set; }
        public IEnumerable<TaskItemResponseDto> UpdatedTasks { get; set; } = Enumerable.Empty<TaskItemResponseDto>();
    }
}
