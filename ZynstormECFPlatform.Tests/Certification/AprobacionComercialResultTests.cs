using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Services.Certification;

namespace ZynstormECFPlatform.Tests.Certification;

public class AprobacionComercialResultTests
{
    [Fact]
    public void Codigo1_EsAprobacionComercialAceptada()
    {
        var result = new DgiiTransmissionResult { Codigo = 1, Estado = "Aprobación comercial aprobada", Mensaje = "" };

        Assert.False(result.Success);
        Assert.True(CertificationExcelService.IsAprobacionComercialAceptada(result));
    }

    [Fact]
    public void Codigo2_EsRechazada()
    {
        var result = new DgiiTransmissionResult { Codigo = 2, Error = "Rechazado: Factura no encontrada para esta Aprobación comercial." };

        Assert.False(CertificationExcelService.IsAprobacionComercialAceptada(result));
    }

    [Fact]
    public void ErrorHttp_EsRechazada()
    {
        var result = new DgiiTransmissionResult { Error = "HTTP 500 Internal Server Error: ..." };

        Assert.False(CertificationExcelService.IsAprobacionComercialAceptada(result));
    }
}
