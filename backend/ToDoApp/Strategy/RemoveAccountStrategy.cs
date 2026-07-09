
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ToDoApp.Data;
using ToDoApp.Models;
using ToDoApp.DTOs;
using ToDoApp.Strategy;

namespace ToDoApp.Strategy
{
    public class RemoveAccountStrategy : IAuthStrategy
    {
        private readonly ToDoAppDbContext _dbContext;
        private readonly ILogger<RemoveAccountStrategy> _logger;

        public string ActionType => "RemoveAccount";

        public RemoveAccountStrategy(ToDoAppDbContext authDbContext, ILogger<RemoveAccountStrategy> logger)
        {
            _dbContext = authDbContext;
            _logger = logger;
        }
        public async Task<AuthResponseDto> ExecuteAsync(string email, string jsonData, int? userId)
        {

            await _dbContext.TaskItems.Where(t => t.UserId == userId).ExecuteDeleteAsync();
            await _dbContext.Categories.Where(c => c.UserId == userId).ExecuteDeleteAsync();

            await _dbContext.UserSessions.Where(s => s.UserId == userId).ExecuteDeleteAsync();
            await _dbContext.UserCredentials.Where(c => c.UserId == userId).ExecuteDeleteAsync();

            await _dbContext.Users.Where(u => u.Id == userId).ExecuteDeleteAsync();

            _logger.LogInformation($"User {userId}, Email: {email} removed their account");

            return new AuthResponseDto { Success = true, Message = "User removed successfully" };

        }
    }
}