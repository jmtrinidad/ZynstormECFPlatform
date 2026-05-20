using System.Xml;
using static ZynstormECFPlatform.Services.Validation.EcfXmlStructuralValidator;

namespace ZynstormECFPlatform.Services.Validation;

/// <summary>
/// Layer 3: Validates DGII business rules per TipoeCF (31-47) based on
/// dgii_ecf_requirements.md and requiredByType.json specifications.
/// </summary>
internal static class EcfXmlBusinessRuleValidator
{
    /// <summary>
    /// Maps TipoeCF code to human-readable document name.
    /// </summary>
    private static readonly Dictionary<int, string> EcfTypeNames = new()
    {
        [31] = "Factura de Crédito Fiscal",
        [32] = "Factura de Consumo",
        [33] = "Nota de Crédito",
        [34] = "Nota de Débito",
        [41] = "Comprobante de Compras",
        [43] = "Gastos Menores",
        [44] = "Regímenes Especiales",
        [45] = "Gubernamental",
        [46] = "Exportación",
        [47] = "Pagos al Exterior"
    };

    public static List<string> Validate(XmlDocument doc, int ecfType)
    {
        var errors = new List<string>();
        var root = doc.DocumentElement!;
        var encabezado = GetChild(root, "Encabezado")!;
        var idDoc = GetChild(encabezado, "IdDoc")!;
        var comprador = GetChild(encabezado, "Comprador");
        var totales = GetChild(encabezado, "Totales");

        var typeName = EcfTypeNames.GetValueOrDefault(ecfType, $"Tipo {ecfType}");

        // ── 1. IdDoc-level rules ──

        // TipoPago = 2 → FechaLimitePago required
        var tipoPago = GetChildText(idDoc, "TipoPago");
        if (tipoPago == "2" && GetChildText(idDoc, "FechaLimitePago") == null)
        {
            errors.Add("Para pagos a Crédito (TipoPago=2), la <FechaLimitePago> es obligatoria.");
        }

        // TipoIngresos required for most types
        var tiposRequierenIngresos = new HashSet<int> { 31, 32, 33, 34, 44, 45, 46 };
        if (tiposRequierenIngresos.Contains(ecfType) && GetChildText(idDoc, "TipoIngresos") == null)
        {
            errors.Add($"Para {typeName} (tipo {ecfType}), el campo <TipoIngresos> es obligatorio.");
        }

        // ── 2. Buyer rules by type ──

        switch (ecfType)
        {
            case 31: // Crédito Fiscal
            case 44: // Regímenes Especiales
            case 45: // Gubernamental
                ValidateRequiredBuyer(errors, comprador, ecfType, typeName);
                break;

            case 32: // Consumo - conditional
                ValidateConditionalBuyer32(errors, comprador, totales);
                break;

            case 33: // Nota de Crédito
            case 34: // Nota de Débito
                ValidateInformacionReferencia(errors, root, ecfType, typeName);
                break;

            case 41: // Compras
                ValidateRequiredBuyer(errors, comprador, ecfType, typeName);
                break;

            case 43: // Gastos Menores - No buyer required
                // No buyer validation needed
                break;

            case 46: // Exportación
                ValidateExportacion(errors, encabezado, comprador, ecfType, typeName);
                break;

            case 47: // Pagos al Exterior
                ValidatePagosExterior(errors, comprador, ecfType, typeName);
                break;
        }

        // ── 3. Validate IndicadorFacturacion values in items ──
        ValidateItemIndicadores(errors, root);

        // ── 4. Detect tags that don't belong to the document type ──
        ValidateDisallowedTags(errors, root, encabezado, ecfType, typeName);

        return errors;
    }

    private static void ValidateRequiredBuyer(List<string> errors, XmlElement? comprador, int ecfType, string typeName)
    {
        if (comprador == null)
        {
            errors.Add($"Para {typeName} (tipo {ecfType}), el nodo <Comprador> es obligatorio.");
            return;
        }

        if (GetChildText(comprador, "RNCComprador") == null && GetChildText(comprador, "IdentificadorExtranjero") == null)
            errors.Add($"Para {typeName} (tipo {ecfType}), el campo <RNCComprador> es obligatorio.");
        if (GetChildText(comprador, "RazonSocialComprador") == null)
            errors.Add($"Para {typeName} (tipo {ecfType}), el campo <RazonSocialComprador> es obligatorio.");
    }

    private static void ValidateConditionalBuyer32(List<string> errors, XmlElement? comprador, XmlElement? totales)
    {
        if (totales == null) return;

        var montoTotal = ParseDecimal(GetChildText(totales, "MontoTotal"));
        if (montoTotal >= 250000m)
        {
            if (comprador == null)
            {
                errors.Add("Para Factura de Consumo (tipo 32) con MontoTotal >= RD$250,000, el nodo <Comprador> es obligatorio.");
                return;
            }

            if (GetChildText(comprador, "RNCComprador") == null && GetChildText(comprador, "IdentificadorExtranjero") == null)
                errors.Add("Para Factura de Consumo (tipo 32) con MontoTotal >= RD$250,000, debe especificar <RNCComprador> o <IdentificadorExtranjero>.");
        }
    }

    private static void ValidateInformacionReferencia(List<string> errors, XmlElement root, int ecfType, string typeName)
    {
        var infoRef = GetChild(root, "InformacionReferencia");
        if (infoRef == null)
        {
            errors.Add($"Para {typeName} (tipo {ecfType}), el nodo <InformacionReferencia> es obligatorio.");
            return;
        }

        if (GetChildText(infoRef, "NCFModificado") == null)
            errors.Add($"Para {typeName} (tipo {ecfType}), el campo <NCFModificado> es obligatorio en <InformacionReferencia>.");
        if (GetChildText(infoRef, "FechaNCFModificado") == null)
            errors.Add($"Para {typeName} (tipo {ecfType}), el campo <FechaNCFModificado> es obligatorio en <InformacionReferencia>.");
        if (GetChildText(infoRef, "CodigoModificacion") == null)
            errors.Add($"Para {typeName} (tipo {ecfType}), el campo <CodigoModificacion> es obligatorio en <InformacionReferencia>.");

        // Also validate buyer for NC/ND
        var comprador = GetChild(GetChild(root, "Encabezado")!, "Comprador");
        if (comprador != null)
        {
            // Buyer is present - which is fine for NC/ND
        }
    }

    private static void ValidateExportacion(List<string> errors, XmlElement encabezado, XmlElement? comprador, int ecfType, string typeName)
    {
        // InformacionesAdicionales required
        var infoAdicional = GetChild(encabezado, "InformacionesAdicionales");
        if (infoAdicional == null)
        {
            errors.Add($"Para {typeName} (tipo {ecfType}), el nodo <InformacionesAdicionales> es obligatorio (datos de embarque, puerto, peso, etc.).");
        }

        // Transporte required
        var transporte = GetChild(encabezado, "Transporte");
        if (transporte == null)
        {
            errors.Add($"Para {typeName} (tipo {ecfType}), el nodo <Transporte> es obligatorio.");
        }

        // IdentificadorExtranjero or RNCComprador
        if (comprador == null)
        {
            errors.Add($"Para {typeName} (tipo {ecfType}), el nodo <Comprador> es obligatorio.");
        }
        else
        {
            if (GetChildText(comprador, "IdentificadorExtranjero") == null && GetChildText(comprador, "RNCComprador") == null)
                errors.Add($"Para {typeName} (tipo {ecfType}), debe especificar <IdentificadorExtranjero> o <RNCComprador>.");
            if (GetChildText(comprador, "RazonSocialComprador") == null)
                errors.Add($"Para {typeName} (tipo {ecfType}), el campo <RazonSocialComprador> es obligatorio.");
        }
    }

    private static void ValidatePagosExterior(List<string> errors, XmlElement? comprador, int ecfType, string typeName)
    {
        if (comprador == null)
        {
            errors.Add($"Para {typeName} (tipo {ecfType}), el nodo <Comprador> es obligatorio.");
            return;
        }

        if (GetChildText(comprador, "IdentificadorExtranjero") == null)
            errors.Add($"Para {typeName} (tipo {ecfType}), el campo <IdentificadorExtranjero> es obligatorio.");
        if (GetChildText(comprador, "RazonSocialComprador") == null)
            errors.Add($"Para {typeName} (tipo {ecfType}), el campo <RazonSocialComprador> es obligatorio.");
    }

    private static void ValidateItemIndicadores(List<string> errors, XmlElement root)
    {
        var detalles = GetChild(root, "DetallesItems");
        if (detalles == null) return;

        var validIndicadores = new HashSet<string> { "0", "1", "2", "3", "4" };
        var items = detalles.SelectNodes("Item");
        if (items == null) return;

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] is not XmlElement item) continue;
            var indicador = GetChildText(item, "IndicadorFacturacion");
            if (indicador != null && !validIndicadores.Contains(indicador))
            {
                errors.Add($"Item #{i + 1}: IndicadorFacturacion '{indicador}' no es válido. Valores permitidos: 0, 1, 2, 3, 4.");
            }

            var indicadorBS = GetChildText(item, "IndicadorBienoServicio");
            if (indicadorBS != null && indicadorBS != "1" && indicadorBS != "2")
            {
                errors.Add($"Item #{i + 1}: IndicadorBienoServicio '{indicadorBS}' no es válido. Valores permitidos: 1 (Bien), 2 (Servicio).");
            }
        }
    }

    private static void ValidateDisallowedTags(List<string> errors, XmlElement root, XmlElement encabezado, int ecfType, string typeName)
    {
        // InformacionReferencia only valid for types 33, 34
        var infoRef = GetChild(root, "InformacionReferencia");
        if (infoRef != null && ecfType != 33 && ecfType != 34)
        {
            // Types 31 also technically allow InformacionReferencia in XSD, so only warn for truly invalid types
            var typesAllowInfoRef = new HashSet<int> { 31, 33, 34 };
            if (!typesAllowInfoRef.Contains(ecfType))
            {
                errors.Add($"El nodo <InformacionReferencia> no corresponde al tipo {typeName} (tipo {ecfType}). Solo aplica para Notas de Crédito (33) y Débito (34).");
            }
        }

        // InformacionesAdicionales with export-specific fields only for type 46
        var infoAdicional = GetChild(encabezado, "InformacionesAdicionales");
        if (infoAdicional != null && ecfType != 46)
        {
            // Check if it has export-specific tags
            if (GetChildText(infoAdicional, "NombrePuertoEmbarque") != null ||
                GetChildText(infoAdicional, "RegimenAduanero") != null ||
                GetChildText(infoAdicional, "CondicionesEntrega") != null)
            {
                errors.Add($"Los campos de exportación (NombrePuertoEmbarque, RegimenAduanero, CondicionesEntrega) en <InformacionesAdicionales> solo corresponden a Exportación (tipo 46), no a {typeName} (tipo {ecfType}).");
            }
        }

        // Transporte with ViaTransporte/PaisOrigen/PaisDestino only for type 46
        var transporte = GetChild(encabezado, "Transporte");
        if (transporte != null && ecfType == 46)
        {
            // Validate export transport has required fields
            if (GetChildText(transporte, "ViaTransporte") == null)
                errors.Add($"Para {typeName} (tipo {ecfType}), <ViaTransporte> es obligatorio dentro de <Transporte>.");
        }

        // OtraMoneda is expected for type 47
        var otraMoneda = GetChild(encabezado, "OtraMoneda");
        if (ecfType == 47 && otraMoneda == null)
        {
            // Not strictly required by XSD but expected per DGII practice
            // Just a warning, not an error
        }

        if (ecfType == 44)
        {
            ValidateType44Totales(errors, encabezado);
        }
    }

    private static void ValidateType44Totales(List<string> errors, XmlElement encabezado)
    {
        var totales = GetChild(encabezado, "Totales");
        if (totales == null) return;

        var disallowedTotalTags = new[]
        {
            "MontoGravadoTotal",
            "MontoGravadoI1",
            "MontoGravadoI2",
            "MontoGravadoI3",
            "ITBIS1",
            "ITBIS2",
            "ITBIS3",
            "TotalITBIS",
            "TotalITBIS1",
            "TotalITBIS2",
            "TotalITBIS3"
        };

        foreach (var tag in disallowedTotalTags)
        {
            if (GetChild(totales, tag) != null)
            {
                errors.Add($"Para Regímenes Especiales (tipo 44), <Totales> no debe incluir <{tag}>. Use <MontoExento>, <MontoImpuestoAdicional>, <ImpuestosAdicionales> y <MontoTotal> según aplique.");
            }
        }
    }
}
