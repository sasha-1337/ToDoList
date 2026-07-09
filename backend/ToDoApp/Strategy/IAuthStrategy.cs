using ToDoApp.DTOs;

namespace ToDoApp.Strategy;

public interface IAuthStrategy
{
	string ActionType { get; }
	Task<AuthResponseDto> ExecuteAsync(string email, string jsonData, int? userId);
}