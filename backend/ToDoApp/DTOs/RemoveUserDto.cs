using System.ComponentModel.DataAnnotations;

namespace ToDoApp.DTOs
{
    public class RemoveUserDto
    {

        [Required(ErrorMessage = "Password is required to confirm deletion")]
        public required string Password { get; set; }
    }
}