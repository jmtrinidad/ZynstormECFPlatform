namespace ZynstormECFPlatform.Core.Enums;

/// <summary>Método con el que se recibió un pago. Se serializa como número.</summary>
public enum PaymentMethod
{
    Cash = 1,
    Transfer = 2,
    Card = 3,
    Check = 4,
    Other = 5
}
