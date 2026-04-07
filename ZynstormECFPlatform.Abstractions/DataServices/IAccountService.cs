using Microsoft.AspNetCore.Identity;
using ZynstormECFPlatform.Common.Enums;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Abstractions.DataServices;

public interface IAccountService
{
    Task<string> GeneratePasswordResetTokenAsync(User user);

    Task<IdentityResult> ResetPasswordAsync(User user, string token, string password);

    Task<string> GenerateEmailConfirmationTokenAsync(User user);

    Task<IdentityResult> ConfirmEmailAsync(User user, string token);

    Task<IdentityResult> ChangePasswordAsync(User user, string oldPassword, string newPassword);

    Task<IdentityResult> UpdateUserAsync(User user);

    Task<User?> GetUserByIdAsync(string userId);

    Task<List<User>> GetAllUsersAsync();

    Task<User?> GetUserByUserNameAsync(string userName);

    Task<Role?> GetRoleByUserAsync(User user);

    Task<User?> AddUserAsync(User user, UserType userType);

    Task<User?> GetUserByEmailAsync(string email);

    Task<IdentityResult> AddUserAsync(User user, string password);

    Task CheckRoleAsync(string roleName);

    Task AddUserToRoleAsync(User user, string roleName);

    Task<bool> IsUserInRoleAsync(User user, string roleName);

    Task<SignInResult> LoginAsync(UserLoginDto model);

    Task LogoutAsync();

    Task<SignInResult> ValidatePasswordAsync(User user, string password);
}