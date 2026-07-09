using Microsoft.EntityFrameworkCore;
using ToDoApp.Models;

namespace ToDoApp.Data
{
    public class ToDoAppDbContext : DbContext
    {
        public ToDoAppDbContext(DbContextOptions<ToDoAppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<UserCredential> UserCredentials { get; set; } 
        public DbSet<UserSession> UserSessions { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<TaskItem> TaskItems { get; set; }
        public DbSet<PendingAction> PendingActions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // --- Налаштування таблиці User ---
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
                entity.HasIndex(e => e.Username).IsUnique(); // Username має бути унікальним

                entity.Property(e => e.Email).IsRequired().HasMaxLength(150);
                entity.HasIndex(e => e.Email).IsUnique(); // Email має бути унікальним
            });

            // Унікальний провайдер для одного користувача
            modelBuilder.Entity<UserCredential>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(c => new { c.UserId, c.AuthProvider }).IsUnique();
            });

            // --- Налаштування таблиці Category ---
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);

                // Зв'язок: Користувач -> Категорії (1:N)
                entity.HasOne(c => c.User)
                      .WithMany(u => u.Categories)
                      .HasForeignKey(c => c.UserId)
                      .OnDelete(DeleteBehavior.NoAction);
            });

            // --- Налаштування таблиці TaskItem ---
            modelBuilder.Entity<TaskItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(1000);

                entity.HasIndex(t => new { t.UserId, t.CategoryId, t.CreatedAt });

                // Зв'язок: Користувач -> Завдання (1:N)
                entity.HasOne(t => t.User)
                      .WithMany(u => u.Tasks)
                      .HasForeignKey(t => t.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Зв'язок: Категорія -> Завдання (1:N)
                entity.HasOne(t => t.Category)
                      .WithMany(c => c.Tasks)
                      .HasForeignKey(t => t.CategoryId)
                      .OnDelete(DeleteBehavior.SetNull);
            });
        }
        
    }
}