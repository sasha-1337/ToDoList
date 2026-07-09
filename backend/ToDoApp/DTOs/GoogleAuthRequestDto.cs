using System.ComponentModel.DataAnnotations;

namespace ToDoApp.DTOs
{
    public class GoogleAuthRequestDto
    {
        [Required(ErrorMessage = "Token is required")]
        public required string IdToken { get; set; }
    }
}