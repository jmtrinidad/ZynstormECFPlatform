using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Services.Ri;
using Xunit;

namespace ZynstormECFPlatform.Tests.Ri;

public class EcfRiTemplateMapperTests
{
    private static string Load(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Ri", "Fixtures", name));

    [Fact]
    public void MapInvoice_Rfce_PopulatesModel_AndUsesFcPortal()
    {
        // Paso_25_E320000000344.xml: RFCE, TipoeCF=32, MontoTotal=6029.00 (<250000),
        // CodigoSeguridadeCF=QuGoOA, no ítems -> resumen consumidor.
        var model = EcfRiTemplateMapper.MapInvoice(Load("Paso_25_E320000000344.xml"), DgiiEnvironment.CerteCF);

        Assert.Equal("E320000000344", model.NcfNumber);
        Assert.Equal(32, model.EcfType);
        Assert.Equal(6029.00m, model.Total);
        Assert.NotEmpty(model.Items);
        Assert.Equal("QuGoOA", model.SecurityCode);
        Assert.Contains("fc.dgii.gov.do/CerteCF/ConsultaTimbreFC", model.Qr);
        Assert.Contains("CodigoSeguridad=QuGoOA", model.Qr);
    }

    [Fact]
    public void MapInvoice_FullEcf_PopulatesModel_AndUsesRegularPortal()
    {
        // Paso_1_E310000000402.xml: ECF, TipoeCF=31, RNCComprador=102620717,
        // 1 <Item>, MontoTotal=6001.00.
        var model = EcfRiTemplateMapper.MapInvoice(Load("Paso_1_E310000000402.xml"), DgiiEnvironment.CerteCF);

        Assert.Equal("E310000000402", model.NcfNumber);
        Assert.Equal(31, model.EcfType);
        Assert.Equal(6001.00m, model.Total);
        Assert.Single(model.Items);
        // EcfRiDataMapper prefers DescripcionItem over NombreItem when both are present.
        Assert.Equal(
            "Factura: 310013315 | Fecha: 01/04/2026 | Cliente: Tiles Import y Export | Origen: Morteros Santiago | Destino: Santiago",
            model.Items[0].Description);
        Assert.Equal("MORTEROS DE EUROPA", model.Client.Name);
        Assert.Equal("102620717", model.Client.Rnc);
        Assert.Contains("ecf.dgii.gov.do/CerteCF/ConsultaTimbre", model.Qr);
        Assert.Contains("RncComprador=102620717", model.Qr);
        Assert.NotEmpty(model.SecurityCode);
    }

    [Fact]
    public void MapInvoice_E31_Credito_PopulatesNewFields()
    {
        // Paso_1_E310000000402.xml: TipoPago=2, TerminoPago="15 DIAS",
        // FechaVencimientoSecuencia=31-12-2028, sin NumeroFacturaInterna,
        // MontoExento=6001 (exento), item con UnidadMedida=43.
        var model = EcfRiTemplateMapper.MapInvoice(Load("Paso_1_E310000000402.xml"), DgiiEnvironment.CerteCF);

        Assert.Equal("31/12/2028", model.ValidUntil);
        Assert.Equal(string.Empty, model.InternalInvoiceNumber);
        Assert.Equal("CRÉDITO", model.PaymentType);
        Assert.True(model.IsCredit);
        Assert.Equal("15 DIAS", model.PaymentCondition);
        Assert.Equal("PEDRO", model.Cashier);
        Assert.Equal(6001.00m, model.SubTotal);
        Assert.Equal("Und", model.Items[0].Unit);
        Assert.Equal(6001.00m, model.ReceivedAmount); // entero -> igual al total
        Assert.Equal(0m, model.ChangeAmount);
    }

    [Fact]
    public void MapInvoice_E34_IsCreditNote_WithReferenceInfo()
    {
        // Paso_E340000000002.xml: TipoeCF=34 (Nota de CRÉDITO según DGII),
        // NCFModificado=E310000000034, CodigoModificacion=3, RazonModificacion="Error en monto".
        var model = EcfRiTemplateMapper.MapInvoice(Load("Paso_E340000000002.xml"), DgiiEnvironment.CerteCF);

        Assert.Equal(34, model.EcfType);
        Assert.Equal("NOTA DE CRÉDITO ELECTRÓNICA", model.NcfTypeName);
        Assert.Equal("E310000000034", model.AffectedNcf);
        Assert.StartsWith("3 - ", model.ModificationCode);
        Assert.Equal("Error en monto", model.ModificationReason);
        Assert.Equal("123456789016", model.InternalInvoiceNumber);
        Assert.Equal(string.Empty, model.ValidUntil);
        Assert.Equal(566695.00m, model.ReceivedAmount);
    }

    [Fact]
    public void MapInvoice_ReceivedAmount_RoundsUpDecimals()
    {
        // E41 fixture reutilizado solo por sus totales: MontoTotal=18955.58 -> recibido 18956, cambio 0.42.
        var model = EcfRiTemplateMapper.MapInvoice(Load("Paso_E410000000007.xml"), DgiiEnvironment.CerteCF);

        Assert.Equal(18956m, model.ReceivedAmount);
        Assert.Equal(0.42m, model.ChangeAmount);
    }

    [Fact]
    public void MapPurchase_E41_CompanyIsEmisor_SupplierIsComprador_WithRetentions()
    {
        // Paso_E410000000007.xml: Emisor=DOCUMENTOS ELECTRONICOS DE 02 (la empresa),
        // Comprador=DOCUMENTOS ELECTRONICOS DE 11 (suplidor informal),
        // TotalITBISRetenido=2846.53, TotalISRRetencion=1606.41, ITBIS1=18.
        var model = EcfRiTemplateMapper.MapPurchase(Load("Paso_E410000000007.xml"), DgiiEnvironment.CerteCF);

        Assert.Equal("DOCUMENTOS ELECTRONICOS DE 02", model.Company.Name);
        Assert.Equal("132293894", model.Company.Rnc);
        Assert.Equal("DOCUMENTOS ELECTRONICOS DE 11", model.Supplier.Name);
        Assert.Equal("533445861", model.Supplier.Rnc);
        Assert.Equal(2846.53m, model.ItbisRetentionAmount);
        Assert.Equal(1606.41m, model.IsrRetentionAmount);
        Assert.Equal(10.0m, Math.Round(model.IsrRetentionRate, 1)); // 1606.41 / 16064.05 * 100
        Assert.Equal(16064.05m, model.SubTotal);
        Assert.Equal(18m, model.Items[0].ItbisRate);
        Assert.Equal(Math.Round(model.Items[0].Amount * 0.18m, 2), model.Items[0].Itbis);
    }

    [Fact]
    public void MapExpense_E43_PopulatesModel()
    {
        // Paso_E430000000008.xml: sin Comprador ni TipoPago; MontoExento=4950;
        // 1 ítem "Gasto personal en comida (kiosko)"; FechaVencimientoSecuencia=31-12-2028.
        var model = EcfRiTemplateMapper.MapExpense(Load("Paso_E430000000008.xml"), DgiiEnvironment.CerteCF);

        Assert.Equal("DOCUMENTOS ELECTRONICOS DE 02", model.Company.Name);
        Assert.Equal("E430000000008", model.NcfNumber);
        Assert.Equal("31/12/2028", model.ValidUntil);
        Assert.Equal(string.Empty, model.PaymentMethod);
        Assert.Equal("PEDRO", model.UserName);
        Assert.Equal("Gasto personal en comida (kiosko)", model.Concept);
        Assert.Equal(4950.00m, model.SubTotal);
        Assert.Equal(0m, model.Itbis);
        Assert.Equal("EXENTO:", model.ItbisLabel);
        Assert.Equal(4950.00m, model.Total);
        Assert.NotEmpty(model.Qr);
        Assert.NotEmpty(model.SecurityCode);
    }
}
