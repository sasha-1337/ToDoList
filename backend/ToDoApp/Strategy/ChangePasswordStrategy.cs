using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ToDoApp.Data;
using ToDoApp.Models;
using ToDoApp.DTOs;

namespace ToDoApp.Strategy
{
    public class ChangePasswordStrategy : IAuthStrategy
    {
        private readonly ToDoAppDbContext _dbContext;
        private readonly PasswordHasher<User> _passwordHasher;
        private readonly ILogger<ChangePasswordStrategy> _logger;

        public string ActionType => "ChangePassword";

        public ChangePasswordStrategy(ToDoAppDbContext dbContext, ILogger<ChangePasswordStrategy> logger)
        {
            _dbContext = dbContext;
            _passwordHasher = new PasswordHasher<User>();
            _logger = logger;
        }

        public async Task<AuthResponseDto> ExecuteAsync(string email, string jsonData, int? userId)
        {
            var data = System.Text.Json.JsonSerializer.Deserialize<ChangePasswordCacheData>(jsonData);

            var credential = await _dbContext.UserCredentials
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.UserId == userId && c.User.Email == email);
            if (credential == null || !credential.User.IsActive)
                return new AuthResponseDto { Success = false, Message = "User not found or inactive" };
            
            credential.PasswordHash = data!.NewPasswordHash;
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation($"User {credential.User.Email} changed password successfully.");
            return new AuthResponseDto { Success = true, Message = "Password changed successfully" };
            //var data = System.Text.Json.JsonSerializer.Deserialize<ChangePasswordCacheData>(jsonData);

            //var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId && u.Email == email);
            //if (user == null || !user.IsActive)
            //    return new AuthResponseDto { Success = false, Message = "User not found or inactive" };

            //user.PasswordHash = data!.NewPasswordHash;
            //await _dbContext.SaveChangesAsync();

            //_logger.LogInformation($"User {user.Email} changed password successfully via strategy.");
            //return new AuthResponseDto { Success = true, Message = "Password changed successfully" };
        }
    }
}

record ChangePasswordCacheData(string NewPasswordHash);