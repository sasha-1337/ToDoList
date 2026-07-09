using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ToDoApp.Models
{
    [Table("Users")]
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public required string Username { get; set; }

        [Required]
        [StringLength(255)]
        [EmailAddress]
        public required string Email { get; set; }

        public bool IsEmailConfirmed { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLogin { get; set; }
        public int TotalScore { get; set; }
        public bool IsActive { get; set; } = true;

        // Навігаційні властивості для підтаблиць авторизації та сесій
        public ICollection<UserCredential> Credentials { get; set; } = new List<UserCredential>();
        public ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();

        // Твої існуючі зв'язки з тасками та категоріями
        public ICollection<Category> Categories { get; set; } = new List<Category>();
        public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    }

        //[Table("Users")]
        //public class User
        //{
        //    [Key]
        //    public int Id { get; set; }

        //    [Required]
        //    [StringLength(50)]
        //    public required string Username { get; set; }

        //    [Required]
        //    [StringLength(255)]
        //    [EmailAddress]
        //    public required string Email { get; set; }
        //    public bool IsEmailConfirmed { get; set; } = false;

        //    public string? PasswordHash { get; set; }

        //    public string AuthProvider { get; set; } = "Local";
        //    public string? ExternalId { get; set; }

        //    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        //    public DateTime? LastLogin { get; set; }
        //    public int TotalScore { get; set; }
        //    public bool IsActive { get; set; } = true;

        //    public string? RefreshToken { get; set; }
        //    public DateTime? RefreshTokenExpiry { get; set; }


        //    // Навігаційні властивості: один користувач має багато категорій та завдань
        //    public ICollection<Category> Categories { get; set; } = new List<Category>();
        //    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();

        
    //}
}