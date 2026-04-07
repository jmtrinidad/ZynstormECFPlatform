using Microsoft.AspNetCore.Identity;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Abstractions.Services
{
    public interface IAuthService
    {
        Task<string> GeneratePasswordResetTokenAsync(User user);

        Task<IdentityResult> ResetPasswordAsync(User user, string token, string password);

        Task<string> GenerateEmailConfirmationTokenAsync(User user);

        Task<IdentityResult> ConfirmEmailAsync(User user, string token);

        Task<IdentityResult> ChangePasswordAsync(User user, string oldPassword, string newPassword);

        Task<IdentityResult> UpdateUserAsync(User user);

        Task<User?> GetUserByIdAsync(string userId);

        Task<User?> GetUserByUserNameAsync(string userName);

        Task<IdentityRole?> GetRoleByUserAsync(User user);

        Task<string> GetRoleNameByUserAsync(User user);

        Task<User> AddUserAsync(User user, string password, RoleType roleType);

        Task<IdentityResult> AddUserAsync(User user, string password);

        Task<User?> GetUserByEmailAsync(string email);

        Task<List<User>> GetAllAsync();

        Task CheckRoleAsync(string roleName);

        Task AddUserToRoleAsync(User user, string roleName);

        Task<bool> IsUserInRoleAsync(User user, string roleName);

        Task<SignInResult> SignInAsync(LoginDto model);

        Task LogoutAsync();

        Task<SignInResult> ValidatePasswordAsync(User user, string password);
    }
}
