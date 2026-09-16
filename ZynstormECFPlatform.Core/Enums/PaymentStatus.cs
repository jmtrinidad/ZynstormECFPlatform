namespace ZynstormECFPlatform.Core.Enums;

/// <summary>Semáforo del próximo pago de renta. Se serializa como número.</summary>
public enum PaymentStatus
{
    NoDate = 0,
    Current = 1,
    DueSoon = 2,
    Overdue = 3
}
