using System.ComponentModel.DataAnnotations;

namespace ZynstormECFPlatform.Dtos;

public class ClientCallBackCreateDto
{
    [Required]
    public int ClientId { get; set; }

    [Required]
    public int? ApiKeyId { get; set; }

    public int? ClientBrancheId { get; set; }

    [Required]
    public string Url { get; set; } = null!;

    public string? Secret { get; set; }
}

public class ClientCallBackUpdateDto : ClientCallBackCreateDto
{
    [Required]
    public int ClientCallBackId { get; set; }
}

public class ClientCallBackViewDto : ClientCallBackUpdateDto
{
    public bool IsActive { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}