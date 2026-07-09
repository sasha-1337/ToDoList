using System.ComponentModel.DataAnnotations;

namespace ToDoApp.DTOs
{
    public class RefreshTokenDto
    {
        [Required]
        public required string AccessToken { get; set; }

        [Required]
        public string? RefreshToken { get; set; }
    }
}