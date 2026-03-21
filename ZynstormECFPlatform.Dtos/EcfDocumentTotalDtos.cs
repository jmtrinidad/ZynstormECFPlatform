using System.ComponentModel.DataAnnotations;

namespace ZynstormECFPlatform.Dtos;

public class EcfDocumentTotalDto
{
    [Required]
    public int EcfDocumentTotalId { get; set; }

    [Required]
    public int EcfDocumentId { get; set; }

    [Range(0, double.MaxValue)]
    public decimal TaxableTotal { get; set; }

    [Range(0, double.MaxValue)]
    public decimal ExemptTotal { get; set; }

    [Range(0, double.MaxValue)]
    public decimal DiscountTotal { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Itbistotal { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Total { get; set; }
}
