
    public class PendingAction
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public int? UserId { get; set; } // null для нових користувачів, ID для тих, хто вже авторизований
        public string Email { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty; // "Register", "RemoveAccount", "ChangePassword" etc.
        public string JsonData { get; set; } = "{}"; // Сюди серіалізуємо унікальні дані (паролі, нікнейми тощо)
        public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddMinutes(15);
    }