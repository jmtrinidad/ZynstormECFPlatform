using System.ComponentModel.DataAnnotations;

namespace ZynstormECFPlatform.Dtos
{
    public class LoginDto
    {
        [Display(Name ="Usuario")]
        [Required(ErrorMessage = "El campo {0}, es requerido.!")]
        public string UserName { get; set; } = null!;

        [Display(Name = "Contraseña")] 
        [Required(ErrorMessage = "El campo {0}, es requerido.!")]
        public string Password { get; set; } = null!;
    }
}
