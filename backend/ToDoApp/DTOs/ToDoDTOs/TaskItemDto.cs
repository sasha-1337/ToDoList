using System.ComponentModel.DataAnnotations;

namespace ToDoApp.DTOs.ToDoDTOs
{

    public class PagedResponseDto<T>
    {
        public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
        public string? NextCursor { get; set; }
        public bool HasMore { get; set; }
    }

    public class TaskItemCreateUpdateDto
    {
        [Required(ErrorMessage = "The task name is mandatory.")]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }
        public int? Score { get; set; } = 0;
        public DateTime? Deadline { get; set; } = null;
        public bool IsCompleted { get; set; }
        public int? CategoryId { get; set; }
    }
    public class UpdateTaskStatusResponseDto
    {
        public TaskItemResponseDto Task { get; set; } = null!;
        public bool IsCompleted { get; set; }
        public int TotalScore { get; set; }
    }

    public class TaskItemResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsCompleted { get; set; }
        public int? Score { get; set; } = 0;
        public DateTime? Deadline { get; set; } = null;
        public DateTime CreatedAt { get; set; }
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
    }
}
