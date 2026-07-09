using ToDoApp.DTOs;
using ToDoApp.Models;

namespace ToDoApp.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto> GetCurrentUserAsync(int userId);
        Task<AuthResponseDto> RegisterAsync(RegisterDto request);
        Task<AuthResponseDto> LoginAsync(LoginDto request);
        Task<AuthResponseDto> GoogleLoginAsync(GoogleAuthRequestDto request);
        Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto request);
        Task<AuthResponseDto> LogoutAsync(int userId);
        Task<AuthResponseDto> ChangePasswordAsync(int userId, string email, ChangePasswordDto request);
        Task<AuthResponseDto> RemoveUserAsync(int userId, RemoveUserDto request);
        Task<AuthResponseDto> VerifyEmailAsync(VerifyEmailDto request);
    }
}
