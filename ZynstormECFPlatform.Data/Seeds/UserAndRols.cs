using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Common.Enums;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Data.Seeds;

public class UserAndRols(IAccountService accountService)
{
    private readonly IAccountService _accountService = accountService;

    public async Task SeedAsync()
    {
        await CheckRolesAsync();
        await CheckUserAsync();
    }

    private async Task CheckUserAsync()
    {
        var email = "jm.trinidad.99@hotmail.com";

        if (await _accountService.GetUserByEmailAsync(email) is null)
        {
            var users = GetDefaultUsers();

            foreach (var item in users)
            {
                var result = await _accountService.AddUserAsync(item, "@dmiN35795@#");

                if (result is not null)
                {
                    if (result.Succeeded)
                    {
                        await _accountService.AddUserToRoleAsync(item, item.UserName!.Equals("jm.trinidad.99@hotmail.com") ? nameof(UserType.SA) : nameof(UserType.Admin));
                    }
                }
            }
        }
    }

    private async Task CheckRolesAsync()
    {
        await _accountService.CheckRoleAsync(UserType.SA.ToString());
        await _accountService.CheckRoleAsync(UserType.Admin.ToString());
        await _accountService.CheckRoleAsync(UserType.Normal.ToString());
    }

    private static List<User> GetDefaultUsers()
    {
        return
             [
                new() {
                Email = "jm.trinidad.99@hotmail.com",
                UserName = "jm.trinidad.99@hotmail.com",
                FirstName = "Jose Miguel",
                LastName = "Trinidad Remigio",
                PhoneNumber = "829-436-0332",
                EmailConfirmed = true,
                IsDeleted=false,
            },

         ];
    }
}