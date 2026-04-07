using Microsoft.AspNetCore.Identity;
using ZynstormECFPlatform.Common.Enums;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Dtos;

using Microsoft.EntityFrameworkCore;
using ZynstormECFPlatform.Abstractions.DataServices;

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
        };

        IdentityResult result = await _userManager.CreateAsync(user, model.PasswordHash!);

        if (result != IdentityResult.Success)
        {
            return null;
        }

        User? newUser = await GetUserByEmailAsync(model.Email!);

        if (newUser != null)
            await AddUserToRoleAsync(newUser, userType.ToString());

        return newUser;
    }

    public async Task<IdentityResult?> AddUserAsync(User user, string password)
    {
        return await _userManager.CreateAsync(user, password);
    }

    public async Task AddUserToRoleAsync(User user, string roleName)
    {
        await _userManager.AddToRoleAsync(user, roleName);
    }

    public async Task<IdentityResult?> ChangePasswordAsync(User user, string oldPassword, string newPassword)
    {
        return await _userManager.ChangePasswordAsync(user, oldPassword, newPassword);
    }

    public async Task CheckRoleAsync(string roleName)
    {
        bool roleExists = await _roleManager.RoleExistsAsync(roleName);

        if (!roleExists)
        {
            await _roleManager.CreateAsync(new Role
            {
                Name = roleName
            });
        }
    }

    public async Task<IdentityResult?> ConfirmEmailAsync(User user, string token)
    {
        return await _userManager.ConfirmEmailAsync(user, token);
    }

    public async Task<string> GenerateEmailConfirmationTokenAsync(User user)
    {
        return await _userManager.GenerateEmailConfirmationTokenAsync(user);
    }

    public async Task<string> GeneratePasswordResetTokenAsync(User user)
    {
        return await _userManager.GeneratePasswordResetTokenAsync(user);
    }

    public async Task<User?> GetUserByIdAsync(string userId)
    {
        return await _userManager.FindByIdAsync(userId.ToString());
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        return await _userManager.Users
            .Where(u => !u.IsDeleted)
            .ToListAsync();
    }

    public async Task<Role?> GetRoleByUserAsync(User user)
    {
        var rolName = (await _userManager.GetRolesAsync(user)).FirstOrDefault();

        if (string.IsNullOrEmpty(rolName))
            return default;

        return await _roleManager.FindByNameAsync(rolName);
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _userManager.FindByEmailAsync(email);
    }

    public async Task<User?> GetUserByUserNameAsync(string userName)
    {
        return await _userManager.FindByNameAsync(userName);
    }

    public async Task<bool> IsUserInRoleAsync(User user, string roleName)
    {
        return await _userManager.IsInRoleAsync(user, roleName);
    }

    public async Task<SignInResult> LoginAsync(UserLoginDto model)
    {
        return await _signInManager.PasswordSignInAsync(
             model.UserName,
             model.Password,
             false,
             false);
    }

    public async Task LogoutAsync()
    {
        await _signInManager.SignOutAsync();
    }

    public async Task<IdentityResult?> ResetPasswordAsync(User user, string token, string password)
    {
        return await _userManager.ResetPasswordAsync(user, token, password);
    }

    public async Task<IdentityResult?> UpdateUserAsync(User user)
    {
        return await _userManager.UpdateAsync(user);
    }

    public async Task<SignInResult> ValidatePasswordAsync(User user, string password)
    {
        return await _signInManager.CheckPasswordSignInAsync(user, password, false);
    }
}