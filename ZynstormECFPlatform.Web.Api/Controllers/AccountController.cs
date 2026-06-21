using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using System.Text;
using System.Security.Claims;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Dtos;
using ZynstormECFPlatform.Web.Api.Helpers;
using ZynstormECFPlatform.Core.Entities;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace ZynstormECFPlatform.Web.Api.Controllers
{
    [ApiVersion("1.0")]
    [Route("v{version:apiVersion}/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IAccountService _accountService;
        private readonly IEncryptedService _encryptedService;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IEmailService _emailService;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
             IAccountService accountService,
            IEncryptedService encryptedService,
            IJwtTokenService jwtTokenService,
            IEmailService emailService,
            IWebHostEnvironment env,
            ILogger<AccountController> logger)
        {
            _accountService = accountService;
            _encryptedService = encryptedService;
            _jwtTokenService = jwtTokenService;
            _emailService = emailService;
            _env = env;
            _logger = logger;
        }

        [AllowAnonymous]
        [HttpPost("forgot-password")]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(503)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Los datos enviados no son válidos.");
            }

            var identifier = dto.Identifier.Trim();

            var user = await _accountService.GetUserByEmailAsync(identifier).ConfigureAwait(false)
                       ?? await _accountService.GetUserByUserNameAsync(identifier).ConfigureAwait(false);

            if (user is null)
                return Ok();

            if (!user.IsActive)
            {
                return BadRequest("Usuario no se encuentra activo.");
            }

            var token = await _accountService.GeneratePasswordResetTokenAsync(user).ConfigureAwait(false);
            var encodedToken = Convert.ToBase64String(Encoding.UTF8.GetBytes(token));

            var callbackUrl = Url.Action(
                action: "ResetPassword",
                controller: "ResetPassword",
                values: new { token = encodedToken, identifier = user.Email ?? user.UserName },
                protocol: Request.Scheme);

            var message = ResetPasswordEmailBuilder.Build(callbackUrl!);
            var recipientEmail = user.Email ?? user.UserName ?? string.Empty;

            if (string.IsNullOrWhiteSpace(recipientEmail))
            {
                return Ok("Se envio el mensaje para restablecer la contraseña a su correo.");
            }

            try
            {
                await _emailService.SendEmailAsync(recipientEmail, "Restablecer contraseña", message).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send reset password email for {Identifier}", identifier);
                return StatusCode(StatusCodes.Status503ServiceUnavailable, "No pudimos enviar el correo de restablecimiento en este momento.");
            }

            return Ok();
        }

        [AllowAnonymous]
        [HttpPost("signIn")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(422)]
        [ProducesResponseType(503)]
        public async Task<ActionResult> SignIn([FromBody] UserLoginDto dto)
        {
            try
            {
                dto.UserName = TryDecrypt(dto.UserName);
                dto.Password = TryDecrypt(dto.Password);

                var user = await _accountService.GetUserByUserNameAsync(dto.UserName).ConfigureAwait(false);

                if (user is null)
                    return NotFound("Usuario o contraseña incorrecta");

                if (!user.IsActive)
                    return NotFound("Usuario no se encuentra activo");

                var passwordCheck = await _accountService.ValidatePasswordAsync(user, dto.Password).ConfigureAwait(false);

                if (!passwordCheck.Succeeded)
                    return NotFound("Usuario o contraseña incorrecta");

                if (user.TwoFactorEnabled)
                {
                    return Ok(new LoginResponseDto
                    {
                        RequiresTwoFactor = true,
                        UserId = user.Id
                    });
                }

                var role = await _accountService.GetRoleByUserAsync(user).ConfigureAwait(false);

                var tokenDto = _jwtTokenService.CreateToken(user, role!);

                // Register access
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                var userAgent = Request.Headers["User-Agent"].ToString();
                await _accountService.RegisterAccessAsync(user.Id, ipAddress, userAgent).ConfigureAwait(false);

                // El JWT viaja SOLO en una cookie httpOnly; nunca en el cuerpo JSON.
                SetAuthCookie(tokenDto);

                return Ok(BuildLoginResponse(user, role, tokenDto));
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, message: exception.Message);
                return StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        }

        [AllowAnonymous]
        [HttpPost("register")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(503)]
        public async Task<IActionResult> Register([FromBody] UserRegisterDto dto)
        {
            try
            {
                var user = new User
                {
                    UserName = dto.UserName,
                    Email = dto.Email,
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    PhoneNumber = dto.PhoneNumber,
                    RegisteredAt = DateTime.Now,
                    IsActive = false
                };

                var result = await _accountService.AddUserAsync(user, dto.Password).ConfigureAwait(false);

                if (!result.Succeeded)
                {
                    return BadRequest(result.Errors);
                }

                await _accountService.AddUserToRoleAsync(user, Common.Enums.UserType.Admin.ToString()).ConfigureAwait(false);

                // Generar link de activación
                var token = await _accountService.GenerateEmailConfirmationTokenAsync(user).ConfigureAwait(false);
                var encodedToken = Convert.ToBase64String(Encoding.UTF8.GetBytes(token));

                var callbackUrl = Url.Action(
                    action: "Activate",
                    controller: "Account",
                    values: new { userId = user.Id, token = encodedToken },
                    protocol: Request.Scheme);

                var message = AccountActivationEmailBuilder.Build(callbackUrl!, user.UserName);

                await _emailService.SendEmailAsync("Zynstorm@hotmail.com", "Nueva Solicitud de Registro - Zynstorm ECF", message).ConfigureAwait(false);

                return Ok("Registro exitoso. Su cuenta está pendiente de activación por un administrador.");
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, message: exception.Message);
                return StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        }

        [AllowAnonymous]
        [HttpGet("activate")]
        public async Task<IActionResult> Activate(string userId, string token)
        {
            try
            {
                var user = await _accountService.GetUserByIdAsync(userId).ConfigureAwait(false);
                if (user == null) return NotFound("Usuario no encontrado.");

                var decodedToken = Encoding.UTF8.GetString(Convert.FromBase64String(token));
                var result = await _accountService.ConfirmEmailAsync(user, decodedToken).ConfigureAwait(false);

                if (result.Succeeded)
                {
                    user.IsActive = true;
                    await _accountService.UpdateUserAsync(user).ConfigureAwait(false);
                    return Ok("Usuario activado correctamente. Ya puede iniciar sesión.");
                }

                return BadRequest("No se pudo activar el usuario. El token puede haber expirado.");
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, message: exception.Message);
                return StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        }

        private string TryDecrypt(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return value;

            try
            {
                return _encryptedService.DecryptString(value);
            }
            catch
            {
                return value;
            }
        }

        [AllowAnonymous]
        [HttpPost("login-2fa")]
        public async Task<IActionResult> Login2Fa([FromBody] TwoFactorLoginDto dto)
        {
            try
            {
                var user = await _accountService.GetUserByIdAsync(dto.UserId).ConfigureAwait(false);
                if (user is null)
                    return NotFound("Usuario no encontrado.");

                if (!user.IsActive)
                    return BadRequest("Usuario no se encuentra activo.");

                var isValid = await _accountService.VerifyTwoFactorTokenAsync(user, dto.Code).ConfigureAwait(false);
                if (!isValid)
                    return BadRequest("Código de autenticación inválido.");

                var role = await _accountService.GetRoleByUserAsync(user).ConfigureAwait(false);
                var tokenDto = _jwtTokenService.CreateToken(user, role!);

                // Register access
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                var userAgent = Request.Headers["User-Agent"].ToString();
                await _accountService.RegisterAccessAsync(user.Id, ipAddress, userAgent).ConfigureAwait(false);

                // El JWT viaja SOLO en una cookie httpOnly; nunca en el cuerpo JSON.
                SetAuthCookie(tokenDto);

                return Ok(BuildLoginResponse(user, role, tokenDto));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Login2Fa");
                return StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        }

        [AllowAnonymous]
        [HttpPost("logout")]
        [ProducesResponseType(200)]
        public IActionResult Logout()
        {
            // Borra la cookie httpOnly en el servidor (no basta con limpiar el estado del cliente).
            Response.Cookies.Delete(AuthCookie.Name, AuthCookie.BuildDeleteOptions(_env));
            return Ok();
        }

        private void SetAuthCookie(TokenDto tokenDto)
        {
            Response.Cookies.Append(
                AuthCookie.Name,
                tokenDto.Token,
                AuthCookie.BuildSetOptions(_env, tokenDto.Expiration));
        }

        private static LoginResponseDto BuildLoginResponse(User user, Role? role, TokenDto tokenDto)
        {
            return new LoginResponseDto
            {
                // Token se omite a propósito: el cliente ya no debe verlo.
                Expiration = tokenDto.Expiration,
                ExpirationAt = tokenDto.Expiration,
                Role = role?.Name,
                RequiresTwoFactor = false,
                User = new UserViewDto
                {
                    UserId = user.Id,
                    UserName = user.UserName!,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email!,
                    PhoneNumber = user.PhoneNumber ?? "",
                    TwoFactorEnabled = user.TwoFactorEnabled,
                    IsActive = user.IsActive,
                    RegisteredAt = user.RegisteredAt
                }
            };
        }

        [Authorize]
        [HttpGet("2fa-setup")]
        public async Task<IActionResult> GetTwoFactorSetup()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var user = await _accountService.GetUserByIdAsync(userId).ConfigureAwait(false);
                if (user == null)
                    return NotFound("Usuario no encontrado.");

                var key = await _accountService.GetAuthenticatorKeyAsync(user).ConfigureAwait(false);
                if (string.IsNullOrEmpty(key))
                {
                    await _accountService.ResetAuthenticatorKeyAsync(user).ConfigureAwait(false);
                    key = await _accountService.GetAuthenticatorKeyAsync(user).ConfigureAwait(false);
                }

                var email = user.Email ?? user.UserName;
                var appName = "Zynstorm ECF";
                if (_env.IsDevelopment())
                {
                    appName = "Zynstorm ECF Dev";
                }
                else if (_env.IsStaging() || _env.EnvironmentName.Equals("Staging", StringComparison.OrdinalIgnoreCase))
                {
                    appName = "Zynstorm ECF Staging";
                }
                var authenticatorUri = $"otpauth://totp/{Uri.EscapeDataString(appName)}:{Uri.EscapeDataString(email!)}?secret={key}&issuer={Uri.EscapeDataString(appName)}&digits=6";

                return Ok(new TwoFactorSetupDto
                {
                    SharedKey = key!,
                    AuthenticatorUri = authenticatorUri
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetTwoFactorSetup");
                return StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        }

        [Authorize]
        [HttpPost("2fa-enable")]
        public async Task<IActionResult> EnableTwoFactor([FromBody] TwoFactorVerifyDto dto)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var user = await _accountService.GetUserByIdAsync(userId).ConfigureAwait(false);
                if (user == null)
                    return NotFound("Usuario no encontrado.");

                var isValid = await _accountService.VerifyTwoFactorTokenAsync(user, dto.Code).ConfigureAwait(false);
                if (!isValid)
                    return BadRequest("Código de verificación incorrecto.");

                var result = await _accountService.SetTwoFactorEnabledAsync(user, true).ConfigureAwait(false);
                if (!result.Succeeded)
                    return BadRequest("No se pudo activar el 2FA.");

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in EnableTwoFactor");
                return StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        }

        [Authorize]
        [HttpPost("2fa-disable")]
        public async Task<IActionResult> DisableTwoFactor([FromBody] TwoFactorVerifyDto dto)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var user = await _accountService.GetUserByIdAsync(userId).ConfigureAwait(false);
                if (user == null)
                    return NotFound("Usuario no encontrado.");

                var isValid = await _accountService.VerifyTwoFactorTokenAsync(user, dto.Code).ConfigureAwait(false);
                if (!isValid)
                    return BadRequest("Código de verificación incorrecto.");

                var result = await _accountService.SetTwoFactorEnabledAsync(user, false).ConfigureAwait(false);
                if (!result.Succeeded)
                    return BadRequest("No se pudo desactivar el 2FA.");

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DisableTwoFactor");
                return StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        }
    }
}