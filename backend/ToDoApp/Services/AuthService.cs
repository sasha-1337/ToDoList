using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ToDoApp.Data;
using ToDoApp.DTOs;
using ToDoApp.EnvSettingModels;
using ToDoApp.Models;
using ToDoApp.Strategy;
using System.CodeDom.Compiler;
using System.IdentityModel.Tokens.Jwt;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ToDoApp.Services
{
    public class AuthService : IAuthService
    {
        private readonly ToDoAppDbContext _dbContext;
        private readonly PasswordHasher<User> _passwordHasher;
        private readonly ILogger<AuthService> _logger;
        private readonly GoogleSettings _googleAuthSettings;
        private readonly IEmailService _emailService;
        private readonly ITokenService _tokenService;

        private readonly Dictionary<string, IAuthStrategy> _strategies;

        private readonly int _tokenExpiryDays = 7;

        public AuthService(ToDoAppDbContext dbContext, IConfiguration configuration, ILogger<AuthService> logger, IEnumerable<IAuthStrategy> strategies,
                           IOptions<GoogleSettings> googleAuthSettings, IEmailService emailService, ITokenService tokenService)
        {
            _dbContext = dbContext;
            _passwordHasher = new PasswordHasher<User>();
            _logger = logger;
            _googleAuthSettings = googleAuthSettings.Value;
            _emailService = emailService;

            _strategies = strategies.ToDictionary(s => s.ActionType);
            _tokenService = tokenService;
        }

        private string GenerateConfirmationCode() => new Random().Next(100000, 999999).ToString();

        public async Task<AuthResponseDto> GetCurrentUserAsync(int userId)
        {
            try
            {
                var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
                if (user == null)
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "User not found or inactive"
                    };
                }
                return new AuthResponseDto
                {
                    Success = true,
                    Message = "User retrieved successfully",
                    User = new UserDto
                    {
                        Id = user.Id,
                        Email = user.Email,
                        Username = user.Username,
                        CreatedAt = user.CreatedAt,
                        TotalScore = user.TotalScore
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error occurred while retrieving current user: {ex.Message}");
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "An error occurred while retrieving the current user. Please try again later."
                };
            }
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto request)
        {
            try
            {

                var credential = _dbContext.UserCredentials.Include(c => c.User)
                                .FirstOrDefault(c => c.User.Email == request.Email && c.AuthProvider == "Local");
                
                if (credential == null || !credential.User.IsActive)
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Invalid email"
                    };
                }

                var user = credential.User;

                var result = _passwordHasher.VerifyHashedPassword(credential.User, credential.PasswordHash, request.Password);
                if (result == PasswordVerificationResult.Failed)
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = $"Invalid password {credential.PasswordHash}, {request.Password}"
                    };
                }

                var token = _tokenService.GenerateJwtToken(user);
                var refreshToken = _tokenService.GenerateRefreshToken();

                var session = new UserSession
                {
                    UserId = user.Id,
                    RefreshToken = refreshToken,
                    RefreshTokenExpiry = DateTime.UtcNow.AddDays(_tokenExpiryDays)
                };

                _dbContext.UserSessions.Add(session);

                await _dbContext.SaveChangesAsync();
                _logger.LogInformation($"User {user.Email} logged in successfully");

                return new AuthResponseDto
                {
                    Success = true,
                    Message = "Login successful",
                    AccessToken = token,
                    RefreshToken = refreshToken,
                    User = new UserDto
                    {
                        Id = user.Id,
                        Email = user.Email,
                        Username = user.Username,
                        CreatedAt = user.CreatedAt
                    }
                };

                //var user = _dbContext.Users.FirstOrDefault(u => u.Email == request.Email);

                //if (user == null || !user.IsActive)
                //{
                //    return new AuthResponseDto
                //    {
                //        Success = false,
                //        Message = "Invalid email"
                //    };
                //}

                //var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
                //if (result == PasswordVerificationResult.Failed)
                //{
                //    return new AuthResponseDto
                //    {
                //        Success = false,
                //        Message = "Invalid password"
                //    };
                //}

                //var token = _tokenService.GenerateJwtToken(user);
                //var refreshToken = _tokenService.GenerateRefreshToken();

                //user.RefreshToken = refreshToken;
                //user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);

                //await _dbContext.SaveChangesAsync();
                //_logger.LogInformation($"User {user.Email} logged in successfully");

                //return new AuthResponseDto
                //{
                //    Success = true,
                //    Message = "Login successful",
                //    AccessToken = token,
                //    RefreshToken = refreshToken,
                //    User = new UserDto
                //    {
                //        Id = user.Id,
                //        Email = user.Email,
                //        Username = user.Username,
                //        CreatedAt = user.CreatedAt
                //    }
                //};

            }
            catch (Exception ex)
            {
                _logger.LogError($"Error occurred during user login: {ex.Message}");
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "An error occurred during login. Please try again later."
                };
            }
        }

        public async Task<AuthResponseDto> GoogleLoginAsync(GoogleAuthRequestDto request)
        {
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings()
                {
                    Audience = [_googleAuthSettings.ClientId]
                };

                var payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, settings);

                var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == payload.Email);

                if (user == null)
                {
                    user = new User
                    {
                        Username = payload.Name,
                        Email = payload.Email,
                        CreatedAt = DateTime.UtcNow,
                        IsEmailConfirmed = true
                    };

                    var credential = new UserCredential
                    {
                        User = user,
                        AuthProvider = "Google",
                        ExternalId = payload.Subject
                    };

                    _dbContext.UserCredentials.Add(credential);
                    _logger.LogInformation("New user created with Google account: {Email}", payload.Email);
                }
                else
                {
                    var hasGoogle = await _dbContext.UserCredentials.AnyAsync(c => c.UserId == user.Id
                                                                            && c.AuthProvider == "Google");
                    if (!hasGoogle)
                    {
                        _dbContext.UserCredentials.Add(
                            new UserCredential
                            {
                                UserId = user.Id,
                                AuthProvider = "Google",
                                ExternalId = payload.Subject
                            });
                    }
                }

                var token = _tokenService.GenerateJwtToken(user);
                var refreshToken = _tokenService.GenerateRefreshToken();

                var session = new UserSession
                {
                    User = user,
                    RefreshToken = refreshToken,
                    RefreshTokenExpiry = DateTime.UtcNow.AddDays(_tokenExpiryDays)
                };
                _dbContext.UserSessions.Add(session);

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Existing user logged in with Google account: {Email}", payload.Email);

                return new AuthResponseDto
                {
                    Success = true,
                    Message = "Google login successful",
                    AccessToken = token,
                    RefreshToken = refreshToken,
                    User = new UserDto
                    {
                        Id = user.Id,
                        Email = user.Email,
                        Username = user.Username,
                        CreatedAt = user.CreatedAt
                    }
                };


                //var settings = new GoogleJsonWebSignature.ValidationSettings()
                //{
                //    Audience = [_googleAuthSettings.ClientId]
                //};

                //var payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, settings);

                //var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == payload.Email);
                //bool isNewUser = false;
                //if (user == null)
                //{
                //    isNewUser = true;
                //    user = new User
                //    {
                //        Email = payload.Email,
                //        Username = payload.Name,
                //        AuthProvider = "Google",
                //        ExternalId = payload.Subject,
                //        CreatedAt = DateTime.UtcNow,
                //        IsEmailConfirmed = true
                //    };
                //    _dbContext.Users.Add(user);
                //}
                //else
                //{
                //    user.LastLogin = DateTime.UtcNow;
                //}

                //var token = _tokenService.GenerateJwtToken(user);
                //var refreshToken = _tokenService.GenerateRefreshToken();

                //user.RefreshToken = refreshToken;
                //user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);

                //await _dbContext.SaveChangesAsync();

                //if (isNewUser)
                //    _logger.LogInformation("New user created with Google account: {Email}", user.Email);
                //else
                //    _logger.LogInformation("Existing user logged in with Google account: {Email}", user.Email);

                //return new AuthResponseDto
                //{
                //    Success = true,
                //    Message = "Google login successful",
                //    AccessToken = token,
                //    RefreshToken = refreshToken,
                //    User = new UserDto
                //    {
                //        Id = user.Id,
                //        Email = user.Email,
                //        Username = user.Username,
                //        CreatedAt = user.CreatedAt
                //    }
                //};
            }
            catch (InvalidJwtException ex)
            {
                _logger.LogWarning("Invalid Google token attempt: {Message}", ex.Message);
                return new AuthResponseDto { Success = false, Message = "Invalid Google token." };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error occurred during Google login: {ex.Message}");
                return new AuthResponseDto { Success = false, Message = "An error occurred during Google login. Please try again later." };
            }
        }
        
        public async Task<AuthResponseDto> LogoutAsync(int userId)
        {
            try
            {
                var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null || !user.IsActive)
                    return new AuthResponseDto { Success = false, Message = $"User not found or bad token {userId}" };

                var sessions = _dbContext.UserSessions.Where(s => s.UserId == userId);
                _dbContext.UserSessions.RemoveRange(sessions);

                user.LastLogin = DateTime.Now;
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation($"User Id: {user.Id} Email: {user.Email} logged out successfully");

                return new AuthResponseDto { Success = true, Message = "Logout successful" };

                //var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
                //if (user == null || !user.IsActive)
                //    return new AuthResponseDto { Success = false, Message = $"User not found or bad token {userId}" };

                //user.LastLogin = DateTime.UtcNow;
                //user.RefreshToken = null;
                //user.RefreshTokenExpiry = null;

                //await _dbContext.SaveChangesAsync();
                //_logger.LogInformation($"User Id: {user.Id} Email: {user.Email} logged out successfully");

                //return new AuthResponseDto { Success = true, Message = "Logout successful" };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error occurred during user logout: {ex.Message}");
                return new AuthResponseDto { Success = false, Message = "An error occurred during logout" };
            }
        }

        public async Task<AuthResponseDto> VerifyEmailAsync(VerifyEmailDto request)
        {
            try
            {
                var action = await _dbContext.PendingActions.FirstOrDefaultAsync(a => a.Email == request.Email && 
                                                                                      a.Code == request.Code && 
                                                                                      a.ExpiresAt > DateTime.UtcNow);

                if (action == null)
                    return new AuthResponseDto { Success = false, Message = "Invalid or expired confirmation code." };

                if (!_strategies.TryGetValue(action.ActionType, out var strategy))
                    return new AuthResponseDto { Success = false, Message = "Invalid action type." };


                var result = await strategy.ExecuteAsync(action.Email, action.JsonData, action.UserId);

                if (result.Success)
                {
                    _dbContext.PendingActions.Remove(action);
                    await _dbContext.SaveChangesAsync();

                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error while checking code: {ex.Message}");
                return new AuthResponseDto { Success = false, Message = "Error while saving. Try later." };
            }
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto request)
        {
            try
            {
                var existingUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email ||
                                                                                   u.Username == request.Username);
                
                if (existingUser != null && existingUser.IsEmailConfirmed)
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "User with the same email or username already exists"
                    };
                }
                
                var confirmationCode = GenerateConfirmationCode();

                var cacheData = new RegisterCacheData(request.Username, request.Password);

                await CreatePendingActionAsync(request.Email, "Register", System.Text.Json.JsonSerializer.Serialize(cacheData), null, confirmationCode);

                await _emailService.SendEmailAsync(request.Email, "Confirmation code", $"Your code: {confirmationCode}");

                _logger.LogInformation($"Code {confirmationCode} has been sent to {request.Email}. Waiting for confirmation.");

                return new AuthResponseDto
                {
                    Success = true,
                    Message = "Confirmation code has been sent on your email. Input it for registration ending."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error occurred during user registration: {ex.Message}");
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "An error occurred during registration. Please try again later."
                };
            }
        }
        
        public async Task<AuthResponseDto> ChangePasswordAsync(int userId, string email, ChangePasswordDto request)
        {
            try
            {
                var credential = await _dbContext.UserCredentials
                    .FirstOrDefaultAsync(c => c.UserId == userId && c.User.Email == email);
                if (credential == null)
                    return new AuthResponseDto { Success = false, Message = "Local credential not found" };

                //var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId && u.Email == email);
                //if (user == null || !user.IsActive)
                //    return new AuthResponseDto { Success = false, Message = "User not found or inactive" };

                var result = _passwordHasher.VerifyHashedPassword(credential.User, credential.PasswordHash, request.OldPassword);
                if (result == PasswordVerificationResult.Failed)
                    return new AuthResponseDto { Success = false, Message = "Entered password is incorrect" };

                var newPasswordHash = _passwordHasher.HashPassword(credential.User, request.NewPassword);
                var cacheData = new ChangePasswordCacheData(newPasswordHash);

                var confirmationCode = GenerateConfirmationCode();

                await CreatePendingActionAsync(email, "ChangePassword", System.Text.Json.JsonSerializer.Serialize(cacheData), userId, confirmationCode);

                await _emailService.SendEmailAsync(email, "Confirmation code for password change", $"Your code: {confirmationCode}");

                return new AuthResponseDto
                {
                    Success = true,
                    Message = "Confirmation code has been sent to your email. Input it to complete password change."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error occurred during password change: {ex.Message}");
                return new AuthResponseDto{ Success = false, Message = "An error occurred during password change." };
            }
        }

        public async Task<AuthResponseDto> RemoveUserAsync(int userId, RemoveUserDto request)
        {
            try
            {
                var credential = await _dbContext.UserCredentials.Include(c => c.User).FirstOrDefaultAsync(c => c.UserId == userId);



                //var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
                //if (user == null)
                //    return new AuthResponseDto { Success = false, Message = "User not found" };
                if (credential == null)
                    return new AuthResponseDto { Success = false, Message = "User not found" };

                var result = _passwordHasher.VerifyHashedPassword(credential.User, credential.PasswordHash, request.Password);
                if (result == PasswordVerificationResult.Failed)
                    return new AuthResponseDto { Success = false, Message = "Entered password is incorrect" };

                var confirmationCode = GenerateConfirmationCode();

                await CreatePendingActionAsync(credential.User.Email, "RemoveAccount", "{}", userId, confirmationCode);

                await _emailService.SendEmailAsync(credential.User.Email, "Confirmation code for account removal", $"Your code: {confirmationCode}");

                return new AuthResponseDto
                {
                    Success = true,
                    Message = "Confirmation code has been sent on your email. Input it to end deleting."
                };
            }
            catch
            {
                _logger.LogInformation($"Error occurred during user removal for user ID {userId}");
                return new AuthResponseDto { Success = false, Message = "An error occurred during user removal" };
            }
        }

        private async Task CreatePendingActionAsync(string email, string actionType, string jsonData, int? userId, string code)
        {
            var oldAction = _dbContext.PendingActions.Where(a => a.Email == email && a.ActionType == actionType);
            _dbContext.PendingActions.RemoveRange(oldAction);

            var pendingAction = new PendingAction
            {
                Email = email,
                ActionType = actionType,
                Code = code,
                JsonData = jsonData,
                UserId = userId,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15)
            };

            _dbContext.PendingActions.Add(pendingAction);
            await _dbContext.SaveChangesAsync();

        }

        public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto request)
        {
            try
            { 
                var principal = _tokenService.GetPrincipalFromExpiredToken(request.AccessToken);

                var userIdClaim = int.Parse(principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? "0");

                var session = await _dbContext.UserSessions
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.RefreshToken == request.RefreshToken);

                if (session == null || !session.User.IsActive || session.RefreshTokenExpiry <= DateTime.UtcNow)
                {
                    return new AuthResponseDto { Success = false, Message = "Invalid or expired token" };
                }

                var user = session.User;
                var newAccessToken = _tokenService.GenerateJwtToken(user);
                var newRefreshToken = _tokenService.GenerateRefreshToken();

                session.RefreshToken = newRefreshToken;
                session.RefreshTokenExpiry = DateTime.UtcNow.AddDays(_tokenExpiryDays);

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation($"Token refreshed for user Id: {user.Id} Email: {user.Email}");


                return new AuthResponseDto
                {
                    Success = true,
                    Message = "Token refreshed successfully",
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshToken,
                    User = new UserDto
                    {
                        Id = user.Id,
                        Email = user.Email,
                        Username = user.Username,
                        CreatedAt = user.CreatedAt,
                        TotalScore = user.TotalScore
                    }
                };

                //    var principal = _tokenService.GetPrincipalFromExpiredToken(request.AccessToken);

                //    var userIdClaim = int.Parse(principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? "0");
                //    var emailClaim = principal?.FindFirst(JwtRegisteredClaimNames.Email)?.Value;

                //    var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userIdClaim && 
                //                                                               u.Email == emailClaim && u.IsActive);

                //    if (user == null)
                //        return new AuthResponseDto{Success = false,Message = "User not found or inactive"};

                //    if (request.RefreshToken != user.RefreshToken || 
                //        user.RefreshTokenExpiry <= DateTime.UtcNow)
                //        return new AuthResponseDto { Success = false, Message = "Invalid token" };


                //    var newAccessToken = _tokenService.GenerateJwtToken(user);
                //    var newRefreshToken = _tokenService.GenerateRefreshToken();

                //    user.RefreshToken = newRefreshToken;

                //    await _dbContext.SaveChangesAsync();

                //    _logger.LogInformation($"Token refreshed for user Id: {user.Id} Email: {user.Email}");

                //    return new AuthResponseDto
                //    {
                //        Success = true,
                //        Message = "Token refreshed successfully",
                //        AccessToken = newAccessToken,
                //        RefreshToken = newRefreshToken,
                //        User = new UserDto
                //        {
                //            Id = user.Id,
                //            Email = user.Email,
                //            Username = user.Username,
                //            CreatedAt = user.CreatedAt
                //        }
                //    };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error occurred during token refresh: {ex.Message}");
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "An error occurred during token refresh. Please log in again."
                };
            }
        }
    }
}