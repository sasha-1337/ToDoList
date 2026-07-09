using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ToDoApp.Models
{
    [Table("UserCredentials")]
    public class UserCredential
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

        [Required]
        [StringLength(20)]
        public string AuthProvider { get; set; } = "Local"; // "Local", "Google"

        // Якщо AuthProvider == "Local", тут лежить хеш. Якщо "Google" — тут null.
        public string? PasswordHash { get; set; }

        // Унікальний суб'єктний ID, який повертає Google (Google User ID)
        public string? ExternalId { get; set; }
    }
}
