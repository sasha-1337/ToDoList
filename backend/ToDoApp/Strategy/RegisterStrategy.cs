using Microsoft.AspNetCore.Identity;
using ToDoApp.Data;
using ToDoApp.Models;
using ToDoApp.DTOs;
using ToDoApp.Strategy;
using Microsoft.EntityFrameworkCore;
using ToDoApp.Services;

public class RegisterStrategy : IAuthStrategy
{
    private readonly ToDoAppDbContext _dbContext;
    private readonly PasswordHasher<User> _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly ILogger<RegisterStrategy> _logger;

    public string ActionType => "Register";

    public RegisterStrategy(ToDoAppDbContext dbContext, ITokenService tokenService, ILogger<RegisterStrategy> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = new PasswordHasher<User>();
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<AuthResponseDto> ExecuteAsync(string email, string jsonData, int? userId)
    {
        var data = System.Text.Json.JsonSerializer.Deserialize<RegisterCacheData>(jsonData);

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id ==userId && u.Email == email);
        if (user != null)
            return new AuthResponseDto { Success = false, Message = "User already exist" };
        
        user = new User
        {
            Email = email,
            Username = data!.Username,
            CreatedAt = DateTime.UtcNow,
            IsEmailConfirmed = true,
        };

        _dbContext.Users.Add(user);

        var credential = new UserCredential
        {
            User = user,
            AuthProvider = "Local",
            PasswordHash = _passwordHasher.HashPassword(user, data.Password)
        };
        _dbContext.UserCredentials.Add(credential);

        var accessToken = _tokenService.GenerateJwtToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken();

        var session = new UserSession
        {
            User = user,
            RefreshToken = refreshToken,
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(7)
        };
        _dbContext.UserSessions.Add(session);

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation($"User registered and verified successfully with email: {user.Email}");

        return new AuthResponseDto
        {
            Success = true,
            Message = "User registered and verified successfully",
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            User = new UserDto { Id = user.Id, Email = user.Email, Username = user.Username, CreatedAt = user.CreatedAt }
        };

        //user.PasswordHash = _passwordHasher.HashPassword(user, data.Password);
        //user.IsEmailConfirmed = true;

        //var token = _tokenService.GenerateJwtToken(user);
        //var refreshToken = _tokenService.GenerateRefreshToken();

        //user.RefreshToken = refreshToken;
        //user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);

        //await _dbContext.SaveChangesAsync();

        //_logger.LogInformation($"User registered and verified successfully with email: {user.Email}");

        //return new AuthResponseDto
        //{
        //    Success = true,
        //    Message = "User registered and verified successfully",
        //    AccessToken = token,
        //    RefreshToken = refreshToken,
        //    User = new UserDto { Id = user.Id, Email = user.Email, Username = user.Username, CreatedAt = user.CreatedAt }
        //};
    }
}

public record RegisterCacheData(string Username, string Password);