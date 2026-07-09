using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ToDoApp.Models
{
    [Table("UserSessions")]
    public class UserSession
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

        [Required]
        [StringLength(500)] // Рефреш-токени бувають довгими, дамо запас
        public string RefreshToken { get; set; } = string.Empty;

        [Required]
        public DateTime RefreshTokenExpiry { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Корисний бонус на майбутнє: можна записувати браузер/пристрій
        [StringLength(255)]
        public string? DeviceInfo { get; set; }
    }
}
