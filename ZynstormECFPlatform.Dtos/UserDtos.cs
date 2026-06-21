using System.ComponentModel.DataAnnotations;
using ZynstormECFPlatform.Common.Enums;

namespace ZynstormECFPlatform.Dtos;

public class UserLoginDto
{
    [Required]
    public string UserName { get; set; } = null!;

    [Required]
    public string Password { get; set; } = null!;
}

public class ChangePasswordDto
{
    [Required]
    public string Id { get; set; } = null!;

    [Required]
    public string OldPassword { get; set; } = null!;

    [Required]
    public string NewPassword { get; set; } = null!;
}

public class LoginResponseDto
{
    public UserViewDto? User { get; set; } = null!;

    // El JWT ya no se devuelve en el cuerpo: viaja únicamente en una cookie httpOnly.
    // Se conserva el campo (siempre null) solo por compatibilidad de serialización.
    public string? Token { get; set; }

    public DateTime? ExpirationAt { get; set; }

    public DateTime? Expiration { get; set; }

    // Nombre del rol (SA/Admin/Normal). El frontend ya no puede leerlo del JWT,
    // así que lo entregamos explícitamente para derivar el userType.
    public string? Role { get; set; }

    public bool RequiresTwoFactor { get; set; }

    public string? UserId { get; set; }
}

public class UserCreateDto
{
    public string UserName { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public int RoleId { get; set; }

    public List<int> ClientIds { get; set; } = [];

    public string? Password { get; set; }
    public bool IsActive { get; set; }
}

public class UserUpdateDto : UserCreateDto
{
    public string UserId { get; set; } = null!;
}

public class UserViewDto : UserUpdateDto
{
    public UserType UserType { get; set; }

    public string FullName => $"{FirstName} {LastName}";

    public DateTime RegisteredAt { get; set; }

    public bool TwoFactorEnabled { get; set; }
}

public class UserRegisterDto : UserCreateDto
{
    [Required]
    public string Password { get; set; } = null!;
}

public class TwoFactorSetupDto
{
    public string SharedKey { get; set; } = null!;
    public string AuthenticatorUri { get; set; } = null!;
}

public class TwoFactorVerifyDto
{
    [Required]
    public string Code { get; set; } = null!;
}

public class TwoFactorLoginDto
{
    [Required]
    public string UserId { get; set; } = null!;

    [Required]
    public string Code { get; set; } = null!;
}