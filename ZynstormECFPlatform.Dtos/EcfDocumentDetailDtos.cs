using System.ComponentModel.DataAnnotations;

namespace ZynstormECFPlatform.Dtos;

public class EcfDocumentDetailDto
{
    [Required]
    public int EcfDocumentDetailId { get; set; }

    [Required]
    public int EcfDocumentId { get; set; }

    [Required]
    public int LineNumber { get; set; }

    [Required]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal Quantity { get; set; }

    [Required]
    public int UnitCode { get; set; }

    [Range(0, double.MaxValue)]
    public decimal UnitPrice { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Discount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal SubTotal { get; set; }

    [Range(0, double.MaxValue)]
    public decimal ItbisPercentage { get; set; }

    [Range(0, double.MaxValue)]
    public decimal ItbisAmount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Total { get; set; }
}
