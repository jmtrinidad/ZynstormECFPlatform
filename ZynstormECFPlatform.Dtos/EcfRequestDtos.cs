using System.ComponentModel.DataAnnotations;

namespace ZynstormECFPlatform.Dtos;

public class EcfInvoiceRequestDto : IValidatableObject
{
    public EcfRequest ECF { get; set; } = new();

    // Internal routing properties that don't go into the XML directly
    public string? ExternalReference { get; set; }

    public string? SecurityCodeOverride { get; set; }
    public DateTime? SignatureDateOverride { get; set; }

    // DGII Certification Helpers
    public DateTime? SequenceExpirationDate { get; set; }

    public decimal? ManualMontoGravadoI1 { get; set; }
    public decimal? ManualMontoGravadoI2 { get; set; }
    public decimal? ManualMontoGravadoI3 { get; set; }
    public string? ReferenceNcf { get; set; }
    public DateTime? ReferenceIssueDate { get; set; }

    // Shortcuts for legacy compatibility in CertificationService
    public List<EcfItemRequestDto> Items => ECF.DetallesItems.Item;

    public string? CustomerRnc => ECF.Encabezado.Comprador.RNCComprador;
    public string? CustomerName => ECF.Encabezado.Comprador.RazonSocialComprador;
    public string? CustomerAddress => ECF.Encabezado.Comprador.DireccionComprador;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var e = ECF?.Encabezado;
        if (e == null)
        {
            yield return new ValidationResult("El objeto ECF.Encabezado es obligatorio.", new[] { nameof(ECF) });
            yield break;
        }

        var tipoEcfStr = e.IdDoc?.TipoeCF;
        if (string.IsNullOrWhiteSpace(tipoEcfStr) && !string.IsNullOrWhiteSpace(e.IdDoc?.eNCF) && e.IdDoc.eNCF.Length >= 3)
        {
            tipoEcfStr = e.IdDoc.eNCF.Substring(1, 2);
        }

        if (!int.TryParse(tipoEcfStr, out int tipoEcf))
        {
            yield return new ValidationResult("No se pudo determinar el TipoeCF. Verifique TipoeCF o eNCF.", new[] { "ECF.Encabezado.IdDoc.TipoeCF" });
            yield break;
        }

        // 1. Validaciones Comunes
        if (string.IsNullOrWhiteSpace(e.Emisor?.RNCEmisor))
            yield return new ValidationResult("El RNC del Emisor es obligatorio.", new[] { "ECF.Encabezado.Emisor.RNCEmisor" });

        if (e.IdDoc?.TipoPago == "2" && string.IsNullOrWhiteSpace(e.IdDoc.FechaLimitePago))
            yield return new ValidationResult("Para pagos a crédito (TipoPago = 2), la FechaLimitePago es obligatoria.", new[] { "ECF.Encabezado.IdDoc.FechaLimitePago" });

        // 2. Validaciones Específicas por TipoeCF
        switch (tipoEcf)
        {
            case 31:
            case 41:
            case 43:
            case 45:
                if (string.IsNullOrWhiteSpace(e.Comprador?.RNCComprador))
                    yield return new ValidationResult($"Para el comprobante tipo {tipoEcf}, el RNCComprador es obligatorio.", new[] { "ECF.Encabezado.Comprador.RNCComprador" });
                if (string.IsNullOrWhiteSpace(e.Comprador?.RazonSocialComprador))
                    yield return new ValidationResult($"Para el comprobante tipo {tipoEcf}, la RazonSocialComprador es obligatoria.", new[] { "ECF.Encabezado.Comprador.RazonSocialComprador" });
                break;

            case 32:
                if (e.Totales?.MontoTotal >= 250000m && string.IsNullOrWhiteSpace(e.Comprador?.RNCComprador) && string.IsNullOrWhiteSpace(e.Comprador?.IdentificadorExtranjero))
                    yield return new ValidationResult("Para Facturas de Consumo >= 250,000, debe especificar RNCComprador o IdentificadorExtranjero.", new[] { "ECF.Encabezado.Comprador" });
                break;

            case 33:
            case 34:
                if (ECF?.InformacionReferencia == null)
                    yield return new ValidationResult($"Para el comprobante {tipoEcf}, el nodo InformacionReferencia es obligatorio.", new[] { "ECF.InformacionReferencia" });
                else
                {
                    if (string.IsNullOrWhiteSpace(ECF.InformacionReferencia.NCFModificado))
                        yield return new ValidationResult("Debe proveer el NCFModificado.", new[] { "ECF.InformacionReferencia.NCFModificado" });
                    if (string.IsNullOrWhiteSpace(ECF.InformacionReferencia.FechaNCFModificado))
                        yield return new ValidationResult("Debe proveer la FechaNCFModificado.", new[] { "ECF.InformacionReferencia.FechaNCFModificado" });
                }
                break;

            case 46:
            case 47:
                if (string.IsNullOrWhiteSpace(e.Comprador?.IdentificadorExtranjero))
                    yield return new ValidationResult($"Para el comprobante {tipoEcf}, el IdentificadorExtranjero es obligatorio.", new[] { "ECF.Encabezado.Comprador.IdentificadorExtranjero" });
                if (tipoEcf == 46 && string.IsNullOrWhiteSpace(e.Comprador?.PaisComprador))
                    yield return new ValidationResult("Para Exportación (46), el PaisComprador es obligatorio.", new[] { "ECF.Encabezado.Comprador.PaisComprador" });
                break;
        }

        if (ECF?.DetallesItems?.Item == null || !ECF.DetallesItems.Item.Any())
            yield return new ValidationResult("El documento debe contener al menos un ítem.", new[] { "ECF.DetallesItems.Item" });
    }
}

public class EcfRequest
{
    public EcfEncabezadoRequest Encabezado { get; set; } = new();
    public EcfDetallesItemsRequest DetallesItems { get; set; } = new();
    public EcfPaginacionRequest? Paginacion { get; set; }
    public string? FechaHoraFirma { get; set; }
    public EcfInformacionReferenciaRequest? InformacionReferencia { get; set; }
}

public class EcfEncabezadoRequest
{
    public string Version { get; set; } = "1.0";
    public EcfIdDocRequest IdDoc { get; set; } = new();
    public EcfEmisorRequest Emisor { get; set; } = new();
    public EcfCompradorRequest Comprador { get; set; } = new();
    public EcfTotalesRequest Totales { get; set; } = new();
}

public class EcfIdDocRequest
{
    public string? TipoeCF { get; set; }
    public string eNCF { get; set; } = null!;
    public string? FechaVencimientoSecuencia { get; set; }
    public string? IndicadorEnvioDiferido { get; set; }
    public string? IndicadorMontoGravado { get; set; }
    public string? TipoIngresos { get; set; }
    public string? TipoPago { get; set; }
    public string? FechaLimitePago { get; set; }
    public int? TotalPaginas { get; set; }
    public string? IndicadorNotaCredito { get; set; }
    public string? TerminoPago { get; set; }
}

public class EcfEmisorRequest
{
    public string RNCEmisor { get; set; } = null!;
    public string RazonSocialEmisor { get; set; } = null!;
    public string DireccionEmisor { get; set; } = null!;
    public string FechaEmision { get; set; } = null!;
    public string? NombreComercial { get; set; }
    public string? Sucursal { get; set; }
    public string? Municipio { get; set; }
    public string? Provincia { get; set; }
    public string? Telefono { get; set; }
    public string? CorreoEmisor { get; set; }
    public string? WebSite { get; set; }
    public string? ActividadEconomica { get; set; }
    public string? CodigoVendedor { get; set; }
    public string? NumeroFacturaInterna { get; set; }
    public string? NumeroPedidoInterno { get; set; }
    public string? ZonaVenta { get; set; }
}

public class EcfCompradorRequest
{
    public string? RNCComprador { get; set; }
    public string? RazonSocialComprador { get; set; }
    public string? IdentificadorExtranjero { get; set; }
    public string? ContactoComprador { get; set; }
    public string? CorreoComprador { get; set; }
    public string? DireccionComprador { get; set; }
    public string? PaisComprador { get; set; }
    public string? TelefonoAdicional { get; set; }
    public string? MunicipioComprador { get; set; }
    public string? ProvinciaComprador { get; set; }
    public string? FechaEntrega { get; set; }
    public string? FechaOrdenCompra { get; set; }
    public string? NumeroOrdenCompra { get; set; }
    public string? CodigoInternoComprador { get; set; }
}

public class EcfTotalesRequest
{
    public decimal? MontoGravadoTotal { get; set; }
    public decimal? MontoGravadoI1 { get; set; }
    public decimal? MontoGravadoI2 { get; set; }
    public decimal? MontoGravadoI3 { get; set; }
    public decimal? MontoExento { get; set; }
    public int? ITBIS1 { get; set; }
    public int? ITBIS2 { get; set; }
    public int? ITBIS3 { get; set; }
    public decimal? TotalITBIS { get; set; }
    public decimal? TotalITBIS1 { get; set; }
    public decimal? TotalITBIS2 { get; set; }
    public decimal? TotalITBIS3 { get; set; }
    public decimal? MontoTotal { get; set; }
    public decimal? MontoNoFacturable { get; set; }
    public decimal? MontoPeriodo { get; set; }
    public decimal? ValorPagar { get; set; }
    public decimal? TotalITBISRetenido { get; set; }
    public decimal? TotalISRRetencion { get; set; }
    public decimal? MontoImpuestoAdicional { get; set; }
}

public class EcfDetallesItemsRequest
{
    public List<EcfItemRequestDto> Item { get; set; } = new();
}

public class EcfItemRequestDto
{
    public string? NumeroLinea { get; set; }
    public string? IndicadorFacturacion { get; set; }
    public string NombreItem { get; set; } = null!;
    public string? IndicadorBienoServicio { get; set; }
    public decimal CantidadItem { get; set; }
    public string? UnidadMedida { get; set; }
    public decimal PrecioUnitarioItem { get; set; }
    public decimal? DescuentoMonto { get; set; }
    public EcfTablaSubDescuentoRequest? TablaSubDescuento { get; set; }
    public decimal MontoItem { get; set; }
    public string? DescripcionItem { get; set; }
    public decimal? RecargoMonto { get; set; }
    public EcfTablaSubRecargoRequest? TablaSubRecargo { get; set; }
    public decimal? MontoITBISRetenido { get; set; }
    public decimal? MontoISRRetenido { get; set; }
    public string? FechaElaboracion { get; set; }
    public string? FechaVencimientoItem { get; set; }

    // Additional Tax Fields
    public string? IscType { get; set; }

    public decimal? IscSpecificAmount { get; set; }
    public decimal? IscAdvaloremAmount { get; set; }
    public decimal? OtherAdditionalTaxAmount { get; set; }
    public decimal? AdditionalTaxRate { get; set; }
}

public class EcfTablaSubDescuentoRequest
{
    public List<EcfSubDescuentoRequest> SubDescuento { get; set; } = new();
}

public class EcfSubDescuentoRequest
{
    public string? TipoSubDescuento { get; set; }
    public decimal? SubDescuentoPorcentaje { get; set; }
    public decimal? MontoSubDescuento { get; set; }
}

public class EcfTablaSubRecargoRequest
{
    public List<EcfSubRecargoRequest> SubRecargo { get; set; } = new();
}

public class EcfSubRecargoRequest
{
    public string? TipoSubRecargo { get; set; }
    public decimal? SubRecargoPorcentaje { get; set; }
    public decimal? MontoSubRecargo { get; set; }
}

public class EcfPaginacionRequest
{
    public List<EcfPaginaRequest> Pagina { get; set; } = new();
}

public class EcfPaginaRequest
{
    public int PaginaNo { get; set; }
    public int NoLineaDesde { get; set; }
    public int NoLineaHasta { get; set; }
    public decimal? SubtotalMontoGravadoPagina { get; set; }
    public decimal? SubtotalMontoGravado1Pagina { get; set; }
    public decimal? SubtotalExentoPagina { get; set; }
    public decimal? SubtotalItbisPagina { get; set; }
    public decimal? SubtotalItbis1Pagina { get; set; }
    public decimal? MontoSubtotalPagina { get; set; }
    public decimal? SubtotalMontoNoFacturablePagina { get; set; }
}

public class EcfInformacionReferenciaRequest
{
    public string? NCFModificado { get; set; }
    public string? RNCOtroContribuyente { get; set; }
    public string? FechaNCFModificado { get; set; }
    public string? CodigoModificacion { get; set; }
    public string? RazonModificacion { get; set; }
}