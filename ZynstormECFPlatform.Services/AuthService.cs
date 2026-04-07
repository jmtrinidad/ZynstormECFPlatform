using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Services
{
    public class AuthService : IDisposable, IAuthService
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<User> _signInManager;

        public AuthService(UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager,
            SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
        }

        public async Task<User> AddUserAsync(User model, string password, RoleType roleType)
        {
            var user = new User
            {
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                PhoneNumber = model.PhoneNumber,
                UserName = model.UserName,
                EmailConfirmed = true,
                IsActive = true,
                RegisteredAt = DateTime.UtcNow
            };

            IdentityResult result = await _userManager.CreateAsync(user, password).ConfigureAwait(false);

            if (result != IdentityResult.Success)
                return null!;

            await AddUserToRoleAsync(user, roleType.ToString()).ConfigureAwait(false);
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
            var roleExists = await _roleManager.RoleExistsAsync(roleName).ConfigureAwait(false);

            if (!roleExists)
            {
                await _roleManager.CreateAsync(new IdentityRole
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

        public async Task<List<User>> GetAllAsync()
        {
            return await _userManager.Users.ToListAsync().ConfigureAwait(false);
        }

        public async Task<IdentityRole?> GetRoleByUserAsync(User user)
        {
            var rolNames = await _userManager.GetRolesAsync(user).ConfigureAwait(false);
            var rolName = rolNames.FirstOrDefault();
            return await _roleManager.FindByNameAsync(rolName!).ConfigureAwait(false);
        }

        public async Task<string> GetRoleNameByUserAsync(User user)
        {
            var rolNames = await _userManager.GetRolesAsync(user).ConfigureAwait(false);
            return rolNames.FirstOrDefault()!;
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

        public async Task<SignInResult> SignInAsync(LoginDto model)
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

        public void Dispose()
        {
            _roleManager.Dispose();
            _userManager.Dispose();
        }
    }
}
