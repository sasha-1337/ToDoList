using System.ComponentModel.DataAnnotations;

namespace ToDoApp.DTOs.ToDoDTOs
{
    public class CategoryCreateUpdateDto
    {
        [Required(ErrorMessage = "The category name is mandatory.")]
        [MaxLength(100, ErrorMessage = "The title cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;
    }

    public class CategoryResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
