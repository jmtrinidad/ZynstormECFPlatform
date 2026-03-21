using System.ComponentModel.DataAnnotations;

namespace ZynstormECFPlatform.Dtos;

public class EcfDocumentCreateDto
{
    [Required]
    public int ClientId { get; set; }

    [Required]
    public int ClientBrancheId { get; set; }

    [Required]
    public int ApiKeyId { get; set; }

    [Required]
    public int EcfTypeId { get; set; }

    [Required]
    [StringLength(100)]
    public string ExternalReference { get; set; } = string.Empty;

    [StringLength(50)]
    public string? Ncf { get; set; }

    [Required]
    [StringLength(15)]
    public string CustomerRnc { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string CustomerName { get; set; } = string.Empty;

    [EmailAddress]
    public string? CustomerEmail { get; set; }

    public string? CustomerAddress { get; set; }

    [Required]
    public DateTime IssueDate { get; set; }

    [Required]
    public int CurrencyId { get; set; }

    [Range(0, double.MaxValue)]
    public decimal SubTotal { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Itbistotal { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Total { get; set; }

    [Required]
    public int EcfStatusId { get; set; }

    public string? HangfireJobId { get; set; }

    public ICollection<EcfDocumentDetailDto> Details { get; set; } = new List<EcfDocumentDetailDto>();

    public ICollection<EcfDocumentTotalDto> Totals { get; set; } = new List<EcfDocumentTotalDto>();
}

public class EcfDocumentUpdateDto : EcfDocumentCreateDto
{
    [Required]
    public int EcfDocumentId { get; set; }
}

public class EcfDocumentViewDto : EcfDocumentUpdateDto
{
    public DateTime CreatedAt { get; set; }

    public EcfStatusDto? Status { get; set; }

    public EcfTypeDto? Type { get; set; }

    public ClientDto? Client { get; set; }

    public ClientBrancheDto? ClientBranche { get; set; }

    public CurrencyDto? Currency { get; set; }
}
