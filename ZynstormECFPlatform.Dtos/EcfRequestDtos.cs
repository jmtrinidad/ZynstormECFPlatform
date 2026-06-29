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

        if (e.IdDoc == null)
        {
            yield return new ValidationResult("El objeto ECF.Encabezado.IdDoc es obligatorio.", new[] { "ECF.Encabezado.IdDoc" });
            yield break;
        }

        if (e.Emisor == null)
        {
            yield return new ValidationResult("El objeto ECF.Encabezado.Emisor es obligatorio.", new[] { "ECF.Encabezado.Emisor" });
            yield break;
        }

        if (e.Totales == null)
        {
            yield return new ValidationResult("El objeto ECF.Encabezado.Totales es obligatorio.", new[] { "ECF.Encabezado.Totales" });
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

        if (string.IsNullOrWhiteSpace(e.IdDoc?.TipoeCF))
            yield return new ValidationResult("El TipoeCF es obligatorio.", new[] { "ECF.Encabezado.IdDoc.TipoeCF" });

        if (string.IsNullOrWhiteSpace(e.IdDoc?.eNCF))
            yield return new ValidationResult("El eNCF es obligatorio.", new[] { "ECF.Encabezado.IdDoc.eNCF" });
        else if (!e.IdDoc.eNCF.StartsWith("E", StringComparison.OrdinalIgnoreCase) || e.IdDoc.eNCF.Length != 13)
            yield return new ValidationResult("El eNCF debe tener el formato E + 2 digitos de tipo + 10 digitos de secuencia.", new[] { "ECF.Encabezado.IdDoc.eNCF" });

        if (string.IsNullOrWhiteSpace(e.Emisor?.RNCEmisor))
            yield return new ValidationResult("El RNC del Emisor es obligatorio.", new[] { "ECF.Encabezado.Emisor.RNCEmisor" });

        if (string.IsNullOrWhiteSpace(e.Emisor?.RazonSocialEmisor))
            yield return new ValidationResult("La RazonSocialEmisor es obligatoria.", new[] { "ECF.Encabezado.Emisor.RazonSocialEmisor" });

        if (string.IsNullOrWhiteSpace(e.Emisor?.DireccionEmisor))
            yield return new ValidationResult("La DireccionEmisor es obligatoria.", new[] { "ECF.Encabezado.Emisor.DireccionEmisor" });

        if (string.IsNullOrWhiteSpace(e.Emisor?.FechaEmision))
            yield return new ValidationResult("La FechaEmision es obligatoria.", new[] { "ECF.Encabezado.Emisor.FechaEmision" });

        if (e.Totales?.MontoTotal == null)
            yield return new ValidationResult("El MontoTotal es obligatorio.", new[] { "ECF.Encabezado.Totales.MontoTotal" });

        int[] tiposRequierenIngresos = { 31, 32, 33, 34, 44, 45, 46 };
        if (tiposRequierenIngresos.Contains(tipoEcf) && string.IsNullOrWhiteSpace(e.IdDoc?.TipoIngresos))
            yield return new ValidationResult($"Para el comprobante tipo {tipoEcf}, el TipoIngresos es obligatorio.", new[] { "ECF.Encabezado.IdDoc.TipoIngresos" });

        if (!string.IsNullOrWhiteSpace(e.IdDoc?.IndicadorMontoGravado))
        {
            if (!IsTipoWithIndicadorMontoGravado(tipoEcf))
            {
                yield return new ValidationResult(
                    $"IndicadorMontoGravado no aplica para el comprobante tipo {tipoEcf}. Omita el campo.",
                    new[] { "ECF.Encabezado.IdDoc.IndicadorMontoGravado" });
            }
            else if (!IsValidIndicadorMontoGravado(e.IdDoc.IndicadorMontoGravado))
            {
                yield return new ValidationResult(
                    "IndicadorMontoGravado debe ser 0 o 1. Use 0 cuando los montos de las lineas no incluyen ITBIS y 1 cuando si lo incluyen.",
                    new[] { "ECF.Encabezado.IdDoc.IndicadorMontoGravado" });
            }
        }

        if (e.IdDoc?.TipoPago == "2" && string.IsNullOrWhiteSpace(e.IdDoc.FechaLimitePago))
            yield return new ValidationResult("Para pagos a credito (TipoPago = 2), la FechaLimitePago es obligatoria.", new[] { "ECF.Encabezado.IdDoc.FechaLimitePago" });

        var formasPago = e.IdDoc?.TablaFormasPago?.FormaDePago;
        var hasFormaPagoShortcut = !string.IsNullOrWhiteSpace(e.IdDoc?.FormaPago);
        if (tipoEcf != 34 && !string.IsNullOrWhiteSpace(e.IdDoc?.TipoPago) && formasPago?.Any() != true && !hasFormaPagoShortcut)
            yield return new ValidationResult("Debe proveer al menos una FormaPago en ECF.Encabezado.IdDoc.TablaFormasPago.FormaDePago.", new[] { "ECF.Encabezado.IdDoc.TablaFormasPago.FormaDePago" });

        if (hasFormaPagoShortcut)
        {
            if (!IsValidPaymentForm(e.IdDoc!.FormaPago))
                yield return new ValidationResult("FormaPago debe estar entre 1 y 8.", new[] { "ECF.Encabezado.IdDoc.FormaPago" });
            if (e.IdDoc.MontoPago == null || e.IdDoc.MontoPago < 0)
                yield return new ValidationResult("MontoPago es obligatorio y no puede ser negativo cuando se usa FormaPago.", new[] { "ECF.Encabezado.IdDoc.MontoPago" });
        }

        if (formasPago?.Any() == true)
        {
            for (var i = 0; i < formasPago.Count; i++)
            {
                var formaPago = formasPago[i];
                var prefix = $"ECF.Encabezado.IdDoc.TablaFormasPago.FormaDePago[{i}]";
                if (!IsValidPaymentForm(formaPago.FormaPago))
                    yield return new ValidationResult($"FormaDePago {i + 1}: FormaPago debe estar entre 1 y 8.", new[] { $"{prefix}.FormaPago" });
                if (formaPago.MontoPago < 0)
                    yield return new ValidationResult($"FormaDePago {i + 1}: MontoPago no puede ser negativo.", new[] { $"{prefix}.MontoPago" });
            }
        }

        switch (tipoEcf)
        {
            case 31:
            case 41:
            case 45:
                if (string.IsNullOrWhiteSpace(e.Comprador?.RNCComprador))
                    yield return new ValidationResult($"Para el comprobante tipo {tipoEcf}, el RNCComprador es obligatorio.", new[] { "ECF.Encabezado.Comprador.RNCComprador" });
                if (string.IsNullOrWhiteSpace(e.Comprador?.RazonSocialComprador))
                    yield return new ValidationResult($"Para el comprobante tipo {tipoEcf}, la RazonSocialComprador es obligatoria.", new[] { "ECF.Encabezado.Comprador.RazonSocialComprador" });
                break;

            case 44:
                if (string.IsNullOrWhiteSpace(e.Comprador?.RNCComprador))
                    yield return new ValidationResult($"Para el comprobante tipo {tipoEcf}, el RNCComprador es obligatorio.", new[] { "ECF.Encabezado.Comprador.RNCComprador" });
                if (string.IsNullOrWhiteSpace(e.Comprador?.RazonSocialComprador))
                    yield return new ValidationResult($"Para el comprobante tipo {tipoEcf}, la RazonSocialComprador es obligatoria.", new[] { "ECF.Encabezado.Comprador.RazonSocialComprador" });
                foreach (var error in ValidateTipo44Totales(e.Totales!))
                    yield return error;
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
                    if (string.IsNullOrWhiteSpace(ECF.InformacionReferencia.CodigoModificacion))
                        yield return new ValidationResult("Debe proveer el CodigoModificacion.", new[] { "ECF.InformacionReferencia.CodigoModificacion" });
                }
                break;

            case 46:
                if (string.IsNullOrWhiteSpace(e.Comprador?.IdentificadorExtranjero) && string.IsNullOrWhiteSpace(e.Comprador?.RNCComprador))
                    yield return new ValidationResult("Para Exportacion (46), debe proveer IdentificadorExtranjero o RNCComprador.", new[] { "ECF.Encabezado.Comprador" });
                if (string.IsNullOrWhiteSpace(e.Comprador?.RazonSocialComprador))
                    yield return new ValidationResult("Para Exportacion (46), la RazonSocialComprador es obligatoria.", new[] { "ECF.Encabezado.Comprador.RazonSocialComprador" });
                if (string.IsNullOrWhiteSpace(e.Comprador?.PaisComprador))
                    yield return new ValidationResult("Para Exportacion (46), el PaisComprador es obligatorio.", new[] { "ECF.Encabezado.Comprador.PaisComprador" });
                break;

            case 47:
                if (string.IsNullOrWhiteSpace(e.Comprador?.IdentificadorExtranjero))
                    yield return new ValidationResult("Para Pagos al Exterior (47), el IdentificadorExtranjero es obligatorio.", new[] { "ECF.Encabezado.Comprador.IdentificadorExtranjero" });
                if (string.IsNullOrWhiteSpace(e.Comprador?.RazonSocialComprador))
                    yield return new ValidationResult("Para Pagos al Exterior (47), la RazonSocialComprador es obligatoria.", new[] { "ECF.Encabezado.Comprador.RazonSocialComprador" });
                break;
        }

        if (ECF?.DetallesItems?.Item == null || !ECF.DetallesItems.Item.Any())
        {
            yield return new ValidationResult("El documento debe contener al menos un item.", new[] { "ECF.DetallesItems.Item" });
            yield break;
        }

        for (var i = 0; i < ECF.DetallesItems.Item.Count; i++)
        {
            var item = ECF.DetallesItems.Item[i];
            var prefix = $"ECF.DetallesItems.Item[{i}]";

            if (string.IsNullOrWhiteSpace(item.NumeroLinea))
                yield return new ValidationResult($"Item {i + 1}: el NumeroLinea es obligatorio.", new[] { $"{prefix}.NumeroLinea" });
            if (string.IsNullOrWhiteSpace(item.IndicadorFacturacion))
                yield return new ValidationResult($"Item {i + 1}: el IndicadorFacturacion es obligatorio.", new[] { $"{prefix}.IndicadorFacturacion" });
            if (string.IsNullOrWhiteSpace(item.NombreItem))
                yield return new ValidationResult($"Item {i + 1}: el NombreItem es obligatorio.", new[] { $"{prefix}.NombreItem" });
            if (item.CantidadItem <= 0)
                yield return new ValidationResult($"Item {i + 1}: la CantidadItem debe ser mayor que cero.", new[] { $"{prefix}.CantidadItem" });
            if (item.PrecioUnitarioItem < 0)
                yield return new ValidationResult($"Item {i + 1}: el PrecioUnitarioItem no puede ser negativo.", new[] { $"{prefix}.PrecioUnitarioItem" });
            if (item.MontoItem <= 0)
                yield return new ValidationResult($"Item {i + 1}: el MontoItem debe ser mayor que cero.", new[] { $"{prefix}.MontoItem" });
            if (!string.IsNullOrWhiteSpace(item.IscType) && item.AdditionalTaxRate <= 0)
                yield return new ValidationResult($"Item {i + 1}: AdditionalTaxRate es obligatorio cuando se especifica IscType.", new[] { $"{prefix}.AdditionalTaxRate" });
        }

        if (ECF?.DescuentosORecargos?.DescuentoORecargo?.Any() == true)
        {
            for (var i = 0; i < ECF.DescuentosORecargos.DescuentoORecargo.Count; i++)
            {
                var ajuste = ECF.DescuentosORecargos.DescuentoORecargo[i];
                var prefix = $"ECF.DescuentosORecargos.DescuentoORecargo[{i}]";

                if (string.IsNullOrWhiteSpace(ajuste.NumeroLinea))
                    yield return new ValidationResult($"DescuentoORecargo {i + 1}: el NumeroLinea es obligatorio.", new[] { $"{prefix}.NumeroLinea" });
                if (ajuste.TipoAjuste != "D" && ajuste.TipoAjuste != "R")
                    yield return new ValidationResult($"DescuentoORecargo {i + 1}: TipoAjuste debe ser D o R.", new[] { $"{prefix}.TipoAjuste" });
                if (ajuste.TipoValor != null && ajuste.TipoValor != "$" && ajuste.TipoValor != "%")
                    yield return new ValidationResult($"DescuentoORecargo {i + 1}: TipoValor debe ser $ o %.", new[] { $"{prefix}.TipoValor" });
                if (ajuste.MontoDescuentooRecargo < 0)
                    yield return new ValidationResult($"DescuentoORecargo {i + 1}: MontoDescuentooRecargo no puede ser negativo.", new[] { $"{prefix}.MontoDescuentooRecargo" });
                if (ajuste.IndicadorFacturacionDescuentooRecargo != null && !new[] { "1", "2", "3", "4" }.Contains(ajuste.IndicadorFacturacionDescuentooRecargo))
                    yield return new ValidationResult($"DescuentoORecargo {i + 1}: IndicadorFacturacionDescuentooRecargo debe ser 1, 2, 3 o 4.", new[] { $"{prefix}.IndicadorFacturacionDescuentooRecargo" });
            }
        }
    }

    private static bool IsValidPaymentForm(string? value) =>
        int.TryParse(value, out var formaPago) && formaPago is >= 1 and <= 8;

    private static bool IsValidIndicadorMontoGravado(string? value) =>
        int.TryParse(value, out var indicador) && indicador is 0 or 1;

    private static bool IsTipoWithIndicadorMontoGravado(int tipoEcf) =>
        tipoEcf is 31 or 32 or 33 or 34 or 41 or 45;

    private static IEnumerable<ValidationResult> ValidateTipo44Totales(EcfTotalesRequest totales)
    {
        var invalidAmounts = new (decimal? Value, string Field)[]
        {
            (totales.MontoGravadoTotal, "MontoGravadoTotal"),
            (totales.MontoGravadoI1, "MontoGravadoI1"),
            (totales.MontoGravadoI2, "MontoGravadoI2"),
            (totales.MontoGravadoI3, "MontoGravadoI3"),
            (totales.TotalITBIS, "TotalITBIS"),
            (totales.TotalITBIS1, "TotalITBIS1"),
            (totales.TotalITBIS2, "TotalITBIS2"),
            (totales.TotalITBIS3, "TotalITBIS3")
        };

        foreach (var (value, field) in invalidAmounts)
        {
            if (value.HasValue && value.Value > 0)
            {
                yield return new ValidationResult(
                    $"Para Regimenes Especiales (tipo 44), Totales no debe incluir {field}. Use MontoExento y MontoTotal segun aplique.",
                    new[] { $"ECF.Encabezado.Totales.{field}" });
            }
        }

        var invalidRates = new (int? Value, string Field)[]
        {
            (totales.ITBIS1, "ITBIS1"),
            (totales.ITBIS2, "ITBIS2"),
            (totales.ITBIS3, "ITBIS3")
        };

        foreach (var (value, field) in invalidRates)
        {
            if (value.HasValue && value.Value > 0)
            {
                yield return new ValidationResult(
                    $"Para Regimenes Especiales (tipo 44), Totales no debe incluir {field}.",
                    new[] { $"ECF.Encabezado.Totales.{field}" });
            }
        }
    }
}

public class EcfRequest
{
    public EcfEncabezadoRequest Encabezado { get; set; } = new();
    public EcfDetallesItemsRequest DetallesItems { get; set; } = new();
    public EcfDescuentosORecargosRequest? DescuentosORecargos { get; set; }
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
    public string? FormaPago { get; set; }
    public decimal? MontoPago { get; set; }
    public EcfTablaFormasPagoRequest? TablaFormasPago { get; set; }
    public int? TotalPaginas { get; set; }
    public string? IndicadorNotaCredito { get; set; }
    public string? TerminoPago { get; set; }
    public string? TipoCuentaPago { get; set; }
    public string? NumeroCuentaPago { get; set; }
    public string? BancoPago { get; set; }
    public string? FechaDesde { get; set; }
    public string? FechaHasta { get; set; }
}

public class EcfTablaFormasPagoRequest
{
    public List<EcfFormaDePagoRequest> FormaDePago { get; set; } = new();
}

public class EcfFormaDePagoRequest
{
    public string? FormaPago { get; set; }
    public decimal MontoPago { get; set; }
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
    public string? ContactoEntrega { get; set; }
    public string? DireccionEntrega { get; set; }
    public string? ResponsablePago { get; set; }
    public string? InformacionAdicionalComprador { get; set; }
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

    // Desglose de impuestos adicionales (ISC) a nivel Totales. Solo lo llena el flujo de
    // certificación por Excel; se lee tal cual del Excel.
    public List<EcfImpuestoAdicionalRequest>? ImpuestosAdicionales { get; set; }
}

public class EcfSubcantidadRequest
{
    public decimal? Subcantidad { get; set; }
    public string? CodigoSubcantidad { get; set; }
}

public class EcfImpuestoAdicionalRequest
{
    public string TipoImpuesto { get; set; } = string.Empty;
    public decimal? TasaImpuestoAdicional { get; set; }
    public decimal? MontoImpuestoSelectivoConsumoEspecifico { get; set; }
    public decimal? MontoImpuestoSelectivoConsumoAdvalorem { get; set; }
    public decimal? OtrosImpuestosAdicionales { get; set; }
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
    public int? PrecioUnitarioItemDecimals { get; set; }
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

    // Reference fields for regulated goods (ISC / alcohol). Only populated by the Excel
    // certification flow; read verbatim from the source Excel.
    public decimal? CantidadReferencia { get; set; }
    public string? UnidadReferencia { get; set; }
    public decimal? GradosAlcohol { get; set; }
    public decimal? PrecioUnitarioReferencia { get; set; }

    // Códigos de tipo de impuesto adicional (ISC) que aplican a esta línea (TipoImpuesto[i][j]).
    public List<string>? ImpuestoAdicionalTipos { get; set; }

    // Tabla de subcantidades de la línea (Subcantidad[i][j] / CodigoSubcantidad[i][j]). Verbatim.
    public List<EcfSubcantidadRequest>? TablaSubcantidad { get; set; }

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

public class EcfDescuentosORecargosRequest
{
    public List<EcfDescuentoORecargoRequest> DescuentoORecargo { get; set; } = new();
}

public class EcfDescuentoORecargoRequest
{
    public string? NumeroLinea { get; set; }
    public string TipoAjuste { get; set; } = "D";
    public string? DescripcionDescuentooRecargo { get; set; }
    public string? TipoValor { get; set; }
    public decimal? ValorDescuentooRecargo { get; set; }
    public decimal? MontoDescuentooRecargo { get; set; }
    public string? IndicadorFacturacionDescuentooRecargo { get; set; }
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
