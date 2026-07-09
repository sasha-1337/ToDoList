using System.ComponentModel.DataAnnotations;

namespace ToDoApp.DTOs
{
    public class ChangePasswordDto
    {
        [Required(ErrorMessage = "Old password is required")]
        public required string OldPassword { get; set; }

        [Required(ErrorMessage = "New password is required")]
        public required string NewPassword { get; set; }
    }
}