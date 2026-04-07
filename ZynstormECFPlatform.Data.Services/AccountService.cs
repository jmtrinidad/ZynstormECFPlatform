using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Common.Enums;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Data.Services;

public class AccountService : IAccountService
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<Role> _roleManager;
    private readonly SignInManager<User> _signInManager;

    public AccountService(
        UserManager<User> userManager,
        RoleManager<Role> roleManager,
        SignInManager<User> signInManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _signInManager = signInManager;
    }

    public async Task<User?> AddUserAsync(User model, UserType userType)
    {
        var user = new User
        {
            Email = model.Email,
            FirstName = model.FirstName,
            LastName = model.LastName,
            PhoneNumber = model.PhoneNumber,
            UserName = model.UserName,
            RegisteredAt = DateTime.UtcNow,
            IsActive = true
        };

        IdentityResult result = await _userManager.CreateAsync(user, model.PasswordHash!).ConfigureAwait(false);

        if (result != IdentityResult.Success)
            return null;

        await AddUserToRoleAsync(user, userType.ToString()).ConfigureAwait(false);
        return user;
    }

    public async Task<IdentityResult> AddUserAsync(User user, string password)
    {
        return await _userManager.CreateAsync(user, password).ConfigureAwait(false);
    }

    public async Task AddUserToRoleAsync(User user, string roleName)
    {
        await _userManager.AddToRoleAsync(user, roleName).ConfigureAwait(false);
    }

    public async Task<IdentityResult> ChangePasswordAsync(User user, string oldPassword, string newPassword)
    {
        return await _userManager.ChangePasswordAsync(user, oldPassword, newPassword).ConfigureAwait(false);
    }

    public async Task CheckRoleAsync(string roleName)
    {
        bool roleExists = await _roleManager.RoleExistsAsync(roleName).ConfigureAwait(false);

        if (!roleExists)
        {
            await _roleManager.CreateAsync(new Role
            {
                Name = roleName
            }).ConfigureAwait(false);
        }
    }

    public async Task<IdentityResult> ConfirmEmailAsync(User user, string token)
    {
        return await _userManager.ConfirmEmailAsync(user, token).ConfigureAwait(false);
    }

    public async Task<string> GenerateEmailConfirmationTokenAsync(User user)
    {
        return await _userManager.GenerateEmailConfirmationTokenAsync(user).ConfigureAwait(false);
    }

    public async Task<string> GeneratePasswordResetTokenAsync(User user)
    {
        return await _userManager.GeneratePasswordResetTokenAsync(user).ConfigureAwait(false);
    }

    public async Task<User?> GetUserByIdAsync(string userId)
    {
        return await _userManager.FindByIdAsync(userId).ConfigureAwait(false);
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        return await _userManager.Users
            .Where(u => !u.IsDeleted)
            .ToListAsync().ConfigureAwait(false);
    }

    public async Task<Role?> GetRoleByUserAsync(User user)
    {
        var rolNames = await _userManager.GetRolesAsync(user).ConfigureAwait(false);
        var rolName = rolNames.FirstOrDefault();

        if (string.IsNullOrEmpty(rolName))
            return default;

        return await _roleManager.FindByNameAsync(rolName).ConfigureAwait(false);
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _userManager.FindByEmailAsync(email).ConfigureAwait(false);
    }

    public async Task<User?> GetUserByUserNameAsync(string userName)
    {
        return await _userManager.FindByNameAsync(userName).ConfigureAwait(false);
    }

    public async Task<bool> IsUserInRoleAsync(User user, string roleName)
    {
        return await _userManager.IsInRoleAsync(user, roleName).ConfigureAwait(false);
    }

    public async Task<SignInResult> LoginAsync(UserLoginDto model)
    {
        return await _signInManager.PasswordSignInAsync(
             model.UserName,
             model.Password,
             false,
             false).ConfigureAwait(false);
    }

    public async Task LogoutAsync()
    {
        await _signInManager.SignOutAsync().ConfigureAwait(false);
    }

    public async Task<IdentityResult> ResetPasswordAsync(User user, string token, string password)
    {
        return await _userManager.ResetPasswordAsync(user, token, password).ConfigureAwait(false);
    }

    public async Task<IdentityResult> UpdateUserAsync(User user)
    {
        return await _userManager.UpdateAsync(user).ConfigureAwait(false);
    }

    public async Task<SignInResult> ValidatePasswordAsync(User user, string password)
    {
        return await _signInManager.CheckPasswordSignInAsync(user, password, false).ConfigureAwait(false);
    }
}