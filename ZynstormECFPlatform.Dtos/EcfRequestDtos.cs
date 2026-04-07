using System.ComponentModel.DataAnnotations;

namespace ZynstormECFPlatform.Dtos;

public class EcfInvoiceRequestDto
{
    [Required]
    public int EcfTypeId { get; set; } // 31, 32, 33, etc.

    [Required]
    public string Ncf { get; set; } = null!;

    [Required]
    public string ExternalReference { get; set; } = null!;

    [Required]
    public DateTime IssueDate { get; set; }

    public DateTime? SequenceExpirationDate { get; set; }

    [Required]
    public string CustomerRnc { get; set; } = null!;

    [Required]
    public string CustomerName { get; set; } = null!;

    public string? CustomerEmail { get; set; }

    public string? CustomerAddress { get; set; }

    public string? CustomerTelephone { get; set; }

    public int PaymentType { get; set; } // 1: Contado, 2: Credito

    public DateTime? PaymentDeadline { get; set; }

    public string? PaymentTerms { get; set; }

    [Required]
    public List<EcfItemRequestDto> Items { get; set; } = [];

    // Totales (Header) - Serán recalculados por el generador si es necesario
    public decimal SubTotal { get; set; }
    public decimal TotalItbis { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal TotalAmount { get; set; }
}

public class EcfItemRequestDto
{
    [Required]
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    [Required]
    public decimal Quantity { get; set; }

    [Required]
    public decimal UnitPrice { get; set; }

    public decimal Discount { get; set; }

    public decimal TaxPercentage { get; set; } // 18, 16, 0
    public decimal ItbisAmount { get; set; }

    public int ItemType { get; set; } // 1: Bien, 2: Servicio

    public int? UnitOfMeasure { get; set; }
}
