using System.ComponentModel.DataAnnotations;

namespace ZynstormECFPlatform.Dtos
{
    public class ForgotPasswordRequestDto
    {
        [Display(Name = "Usuario o correo electrónico")]
        [Required(ErrorMessage = "El campo {0}, es requerido.!")]
        public string Identifier { get; set; } = null!;
    }

    public class ForgotPasswordResponseDto
    {
        public string Token { get; set; } = null!;
    }

    public class ResetPasswordRequestDto
    {
        [Display(Name = "Usuario o correo electrónico")]
        [Required(ErrorMessage = "El campo {0}, es requerido.!")]
        public string Identifier { get; set; } = null!;

        [Required(ErrorMessage = "El token es requerido.")]
        public string Token { get; set; } = null!;

        [Display(Name = "Contraseña")]
        [Required(ErrorMessage = "El campo {0}, es requerido.!")]
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos {1} caracteres.")]
        public string Password { get; set; } = null!;

        [Display(Name = "Confirmar contraseña")]
        [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden.")]
        public string ConfirmPassword { get; set; } = null!;
    }
}
