using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace ZynstormECFPlatform.Data.Extensions
{
    public class PasswordResetTokenProvider<TUser> : DataProtectorTokenProvider<TUser> where TUser : class
    {
        public PasswordResetTokenProvider(
            IDataProtectionProvider dataProtectionProvider,
            IOptions<DataProtectionTokenProviderOptions> options,
            ILogger<DataProtectorTokenProvider<TUser>> logger)
            : base(dataProtectionProvider, options, logger)
        {
        }
    }
}
