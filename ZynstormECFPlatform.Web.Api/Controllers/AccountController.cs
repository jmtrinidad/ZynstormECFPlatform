using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Dtos;
using ZynstormECFPlatform.Web.Api.Helpers;

namespace ZynstormECFPlatform.Web.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IEncryptedService _encryptedService;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IEmailService _emailService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
             IAuthService authService,
            IEncryptedService encryptedService,
            IJwtTokenService jwtTokenService,
            IEmailService emailService,
            ILogger<AccountController> logger)
        {
            _authService = authService;
            _encryptedService = encryptedService;
            _jwtTokenService = jwtTokenService;
            _emailService = emailService;
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

            var user = await _authService.GetUserByEmailAsync(identifier).ConfigureAwait(false)
                       ?? await _authService.GetUserByUserNameAsync(identifier).ConfigureAwait(false);

            if (user is null)
                return Ok();

            if (!user.IsActive)
            {
                return BadRequest("Usuario no se encuentra activo.");
            }

            var token = await _authService.GeneratePasswordResetTokenAsync(user).ConfigureAwait(false);
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
        public async Task<ActionResult> SignIn([FromBody] LoginDto dto)
        {
            try
            {
                dto.UserName = TryDecrypt(dto.UserName);
                dto.Password = TryDecrypt(dto.Password);

                var user = await _authService.GetUserByUserNameAsync(dto.UserName).ConfigureAwait(false);

                if (user is null)
                    return NotFound("Usuario o contraseña incorrecta");

                if (!user.IsActive)
                    return NotFound("Usuario no se encuentra activo");

                var signIn = await _authService.SignInAsync(dto).ConfigureAwait(false);

                if (!signIn.Succeeded)
                    return NotFound("Usuario o contraseña incorrecta");

                var role = await _authService.GetRoleByUserAsync(user).ConfigureAwait(false);

                var tokenDto = _jwtTokenService.CreateToken(user, role!);

                return Ok(tokenDto);
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
    }
}
