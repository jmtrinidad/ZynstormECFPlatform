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
    public UserViewDto User { get; set; } = null!;

    public string Token { get; set; } = null!;

    public DateTime ExpirationAt { get; set; }
}

public class UserCreateDto
{
    public string UserName { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public int RoleId { get; set; }
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
}