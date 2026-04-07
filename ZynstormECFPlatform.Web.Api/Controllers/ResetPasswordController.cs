using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text;
using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Core;
using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Web.Api.Controllers
{
    [AllowAnonymous]
    [Route("[controller]")]
    public class ResetPasswordController : Controller
    {
        private readonly IAuthService _authService;
        private readonly ILogger<ResetPasswordController> _logger;
        private readonly IEmailService _emailService;
        private readonly AppSettings _appSettings;

        public ResetPasswordController(
            IAuthService authService,
            ILogger<ResetPasswordController> logger,
            IEmailService emailService,
            IOptions<AppSettings> appSettings)
        {
            _authService = authService;
            _logger = logger;
            _emailService = emailService;
            _appSettings = appSettings.Value;
        }

        [HttpGet("reset-password")]
        public IActionResult ResetPassword([FromQuery] string? identifier, [FromQuery] string? token)
        {
            var decodedToken = token ?? string.Empty;
            try
            {
                decodedToken = Encoding.UTF8.GetString(Convert.FromBase64String(token ?? string.Empty));
            }
            catch
            {
                // si no viene base64, usar tal cual
            }

            var model = new ResetPasswordRequestDto
            {
                Identifier = identifier ?? string.Empty,
                Token = decodedToken
            };

            return View("ResetPassword", model);
        }

        [HttpPost("reset-password")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                return View("ResetPassword", dto);
            }

            try
            {
                var identifier = dto.Identifier.Trim();

                var user = await _authService.GetUserByEmailAsync(identifier).ConfigureAwait(false)
                           ?? await _authService.GetUserByUserNameAsync(identifier).ConfigureAwait(false);

                if (user is null)
                    return Ok();

                if (!user.IsActive)
                {
                    ModelState.AddModelError(string.Empty, "Usuario no se encuentra activo.");
                    return View("ResetPassword", dto);
                }

                if (dto.Password != dto.ConfirmPassword)
                {
                    ModelState.AddModelError(nameof(dto.ConfirmPassword), "La confirmación no coincide.");
                    return View("ResetPassword", dto);
                }

                var decodedToken = dto.Token;
                try
                {
                    decodedToken = Encoding.UTF8.GetString(Convert.FromBase64String(dto.Token));
                }
                catch
                {
                    // si no viene base64, usar el valor original
                }

                var result = await _authService.ResetPasswordAsync(user, decodedToken, dto.Password).ConfigureAwait(false);

                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        var customDescription = error.Code == "InvalidToken"
                            ? "El enlace para restablecer la contraseña ha expirado o ya no es válido. Solicita uno nuevo para continuar."
                            : error.Description;

                        ModelState.AddModelError(string.Empty, customDescription);
                    }

                    return View("ResetPassword", dto);
                }

                return View("ResetPasswordConfirmation", _appSettings.PasswordChangeSuccessUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                ModelState.AddModelError(string.Empty, "Ocurrió un error al restablecer la contraseña.");
                return View("ResetPassword", dto);
            }
        }

        [HttpGet("reset-password-confirmation")]
        public IActionResult ResetPasswordConfirmation()
        {
            return View("ResetPasswordConfirmation", _appSettings.PasswordChangeSuccessUrl);
        }
    }
}
