namespace ToDoApp.DTOs
{
    public class AuthResponseDto
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public UserDto? User { get; set; }
    }

    public class UserDto
    {
        public int Id { get; set; }
        public string? Email { get; set; }
        public string? Username { get; set; }
        public DateTime CreatedAt { get; set; }
        public int TotalScore { get; set; }
    }

}
