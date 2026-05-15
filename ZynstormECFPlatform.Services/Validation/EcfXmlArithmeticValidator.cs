using System.Xml;
using static ZynstormECFPlatform.Services.Validation.EcfXmlStructuralValidator;

namespace ZynstormECFPlatform.Services.Validation;

/// <summary>
/// Layer 4: Validates arithmetic consistency of totals, ITBIS calculations,
/// item amounts, discounts, and retention sums against DGII requirements.
/// </summary>
internal static class EcfXmlArithmeticValidator
{
    /// <summary>
    /// Tolerance for decimal comparisons (2 centavos).
    /// DGII uses a similar margin in their real validations.
    /// </summary>
    private const decimal Tolerance = 0.02m;

    public static List<string> Validate(XmlDocument doc, int ecfType)
    {
        var errors = new List<string>();
        var root = doc.DocumentElement!;
        var encabezado = GetChild(root, "Encabezado")!;
        var totales = GetChild(encabezado, "Totales");
        var detalles = GetChild(root, "DetallesItems");

        if (totales == null || detalles == null)
            return errors; // Structural validator already caught this

        var items = detalles.SelectNodes("Item");
        if (items == null || items.Count == 0) return errors;

        // ── 1. Validate individual item amounts ──
        ValidateItemAmounts(errors, items);

        // ── 2. Validate sums by IndicadorFacturacion ──
        ValidateIndicadorSums(errors, items, totales);

        // ── 3. Validate MontoGravadoTotal = I1 + I2 + I3 ──
        ValidateMontoGravadoTotal(errors, totales);

        // ── 4. Validate ITBIS calculations ──
        ValidateItbisCalculations(errors, totales);

        // ── 5. Validate TotalITBIS = sum of ITBIS tiers ──
        ValidateTotalItbis(errors, totales);

        // ── 6. Validate MontoTotal ──
        ValidateMontoTotal(errors, totales);

        // ── 7. Validate retention sums ──
        ValidateRetentionSums(errors, items, totales);

        // ── 8. Validate DescuentosORecargos global section ──
        ValidateDescuentosORecargos(errors, root);

        return errors;
    }

    private static void ValidateItemAmounts(List<string> errors, XmlNodeList items)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] is not XmlElement item) continue;
            int lineNum = i + 1;

            var cantidad = ParseDecimal(GetChildText(item, "CantidadItem"));
            var precio = ParseDecimal(GetChildText(item, "PrecioUnitarioItem"));
            var descuento = ParseDecimal(GetChildText(item, "DescuentoMonto"));
            var recargo = ParseDecimal(GetChildText(item, "RecargoMonto"));
            var montoItem = ParseDecimal(GetChildText(item, "MontoItem"));

            var expected = (cantidad * precio) - descuento + recargo;

            if (Math.Abs(expected - montoItem) > Tolerance && expected > 0)
            {
                errors.Add($"Item #{lineNum}: MontoItem ({montoItem:F2}) no coincide con CantidadItem ({cantidad:F2}) × PrecioUnitarioItem ({precio:F4}) - DescuentoMonto ({descuento:F2}) + RecargoMonto ({recargo:F2}) = {expected:F2}.");
            }
        }
    }

    private static void ValidateIndicadorSums(List<string> errors, XmlNodeList items, XmlElement totales)
    {
        decimal sumI1 = 0, sumI2 = 0, sumI3 = 0, sumExento = 0, sumNoFacturable = 0;

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] is not XmlElement item) continue;
            var indicador = GetChildText(item, "IndicadorFacturacion");
            var monto = ParseDecimal(GetChildText(item, "MontoItem"));

            switch (indicador)
            {
                case "0": sumNoFacturable += monto; break;
                case "1": sumI1 += monto; break;
                case "2": sumI2 += monto; break;
                case "3": sumI3 += monto; break;
                case "4": sumExento += monto; break;
            }
        }

        // Account for global discounts/recargos
        // These are checked separately; we compare against what's declared in Totales.

        var declaredI1 = ParseDecimal(GetChildText(totales, "MontoGravadoI1"));
        var declaredI2 = ParseDecimal(GetChildText(totales, "MontoGravadoI2"));
        var declaredI3 = ParseDecimal(GetChildText(totales, "MontoGravadoI3"));
        var declaredExento = ParseDecimal(GetChildText(totales, "MontoExento"));

        // Only validate if the totals are declared (not all are always present)
        if (declaredI1 > 0 && Math.Abs(sumI1 - declaredI1) > Tolerance)
            errors.Add($"Sumatoria de MontoItem con IndicadorFacturacion=1 ({sumI1:F2}) no coincide con MontoGravadoI1 ({declaredI1:F2}) en Totales.");

        if (declaredI2 > 0 && Math.Abs(sumI2 - declaredI2) > Tolerance)
            errors.Add($"Sumatoria de MontoItem con IndicadorFacturacion=2 ({sumI2:F2}) no coincide con MontoGravadoI2 ({declaredI2:F2}) en Totales.");

        if (declaredI3 > 0 && Math.Abs(sumI3 - declaredI3) > Tolerance)
            errors.Add($"Sumatoria de MontoItem con IndicadorFacturacion=3 ({sumI3:F2}) no coincide con MontoGravadoI3 ({declaredI3:F2}) en Totales.");

        if (declaredExento > 0 && Math.Abs(sumExento - declaredExento) > Tolerance)
            errors.Add($"Sumatoria de MontoItem con IndicadorFacturacion=4 ({sumExento:F2}) no coincide con MontoExento ({declaredExento:F2}) en Totales.");
    }

    private static void ValidateMontoGravadoTotal(List<string> errors, XmlElement totales)
    {
        var declaredTotal = ParseDecimal(GetChildText(totales, "MontoGravadoTotal"));
        if (declaredTotal == 0) return; // Not always present

        var i1 = ParseDecimal(GetChildText(totales, "MontoGravadoI1"));
        var i2 = ParseDecimal(GetChildText(totales, "MontoGravadoI2"));
        var i3 = ParseDecimal(GetChildText(totales, "MontoGravadoI3"));
        var expected = i1 + i2 + i3;

        if (Math.Abs(expected - declaredTotal) > Tolerance)
        {
            errors.Add($"MontoGravadoTotal ({declaredTotal:F2}) no coincide con MontoGravadoI1 ({i1:F2}) + MontoGravadoI2 ({i2:F2}) + MontoGravadoI3 ({i3:F2}) = {expected:F2}.");
        }
    }

    private static void ValidateItbisCalculations(List<string> errors, XmlElement totales)
    {
        // ITBIS1 = 18%, ITBIS2 = 16%, ITBIS3 = 0%
        ValidateItbisTier(errors, totales, "MontoGravadoI1", "ITBIS1", "TotalITBIS1", 18, "ITBIS1 (18%)");
        ValidateItbisTier(errors, totales, "MontoGravadoI2", "ITBIS2", "TotalITBIS2", 16, "ITBIS2 (16%)");
        // ITBIS3 is 0% so TotalITBIS3 should be 0
        var totalItbis3 = ParseDecimal(GetChildText(totales, "TotalITBIS3"));
        if (totalItbis3 > Tolerance)
        {
            errors.Add($"TotalITBIS3 ({totalItbis3:F2}) debe ser 0.00 ya que la tasa ITBIS3 es 0%.");
        }
    }

    private static void ValidateItbisTier(List<string> errors, XmlElement totales,
        string montoGravadoField, string tasaField, string totalItbisField, int defaultRate, string tierName)
    {
        var montoGravado = ParseDecimal(GetChildText(totales, montoGravadoField));
        var declaredTotal = ParseDecimal(GetChildText(totales, totalItbisField));

        if (montoGravado == 0 && declaredTotal == 0) return; // Nothing to validate

        var tasa = ParseDecimal(GetChildText(totales, tasaField));
        if (tasa == 0) tasa = defaultRate; // Use default rate if not explicitly declared

        var expected = Math.Round(montoGravado * tasa / 100, 2);

        if (Math.Abs(expected - declaredTotal) > Tolerance)
        {
            errors.Add($"{totalItbisField} ({declaredTotal:F2}) no coincide con {montoGravadoField} ({montoGravado:F2}) × {tasa}% = {expected:F2}.");
        }
    }

    private static void ValidateTotalItbis(List<string> errors, XmlElement totales)
    {
        var totalItbis = ParseDecimal(GetChildText(totales, "TotalITBIS"));
        if (totalItbis == 0 && GetChildText(totales, "TotalITBIS") == null) return;

        var t1 = ParseDecimal(GetChildText(totales, "TotalITBIS1"));
        var t2 = ParseDecimal(GetChildText(totales, "TotalITBIS2"));
        var t3 = ParseDecimal(GetChildText(totales, "TotalITBIS3"));
        var expected = t1 + t2 + t3;

        if (Math.Abs(expected - totalItbis) > Tolerance)
        {
            errors.Add($"TotalITBIS ({totalItbis:F2}) no coincide con TotalITBIS1 ({t1:F2}) + TotalITBIS2 ({t2:F2}) + TotalITBIS3 ({t3:F2}) = {expected:F2}.");
        }
    }

    private static void ValidateMontoTotal(List<string> errors, XmlElement totales)
    {
        var montoTotal = ParseDecimal(GetChildText(totales, "MontoTotal"));
        if (montoTotal == 0) return;

        var gravadoTotal = ParseDecimal(GetChildText(totales, "MontoGravadoTotal"));
        var exento = ParseDecimal(GetChildText(totales, "MontoExento"));
        var totalItbis = ParseDecimal(GetChildText(totales, "TotalITBIS"));
        var impuestoAdicional = ParseDecimal(GetChildText(totales, "MontoImpuestoAdicional"));

        // Only validate if we have component breakdown
        if (gravadoTotal == 0 && exento == 0 && totalItbis == 0) return;

        var expected = gravadoTotal + exento + totalItbis + impuestoAdicional;

        if (Math.Abs(expected - montoTotal) > Tolerance)
        {
            errors.Add($"MontoTotal ({montoTotal:F2}) no coincide con MontoGravadoTotal ({gravadoTotal:F2}) + MontoExento ({exento:F2}) + TotalITBIS ({totalItbis:F2}) + MontoImpuestoAdicional ({impuestoAdicional:F2}) = {expected:F2}.");
        }
    }

    private static void ValidateRetentionSums(List<string> errors, XmlNodeList items, XmlElement totales)
    {
        decimal sumItbisRetenido = 0, sumIsrRetenido = 0;

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] is not XmlElement item) continue;
            var retencion = GetChild(item, "Retencion");
            if (retencion == null) continue;

            sumItbisRetenido += ParseDecimal(GetChildText(retencion, "MontoITBISRetenido"));
            sumIsrRetenido += ParseDecimal(GetChildText(retencion, "MontoISRRetenido"));
        }

        var declaredItbisRetenido = ParseDecimal(GetChildText(totales, "TotalITBISRetenido"));
        if (declaredItbisRetenido > 0 && Math.Abs(sumItbisRetenido - declaredItbisRetenido) > Tolerance)
        {
            errors.Add($"TotalITBISRetenido ({declaredItbisRetenido:F2}) no coincide con la sumatoria de MontoITBISRetenido en items ({sumItbisRetenido:F2}).");
        }

        var declaredIsrRetencion = ParseDecimal(GetChildText(totales, "TotalISRRetencion"));
        if (declaredIsrRetencion > 0 && Math.Abs(sumIsrRetenido - declaredIsrRetencion) > Tolerance)
        {
            errors.Add($"TotalISRRetencion ({declaredIsrRetencion:F2}) no coincide con la sumatoria de MontoISRRetenido en items ({sumIsrRetenido:F2}).");
        }
    }

    private static void ValidateDescuentosORecargos(List<string> errors, XmlElement root)
    {
        var descuentos = GetChild(root, "DescuentosORecargos");
        if (descuentos == null) return;

        var drItems = descuentos.SelectNodes("DescuentoORecargo");
        if (drItems == null) return;

        for (int i = 0; i < drItems.Count; i++)
        {
            if (drItems[i] is not XmlElement dr) continue;
            int lineNum = i + 1;

            var tipoAjuste = GetChildText(dr, "TipoAjuste");
            if (tipoAjuste != null && tipoAjuste != "D" && tipoAjuste != "R")
            {
                errors.Add($"DescuentoORecargo #{lineNum}: TipoAjuste '{tipoAjuste}' no es válido. Debe ser 'D' (Descuento) o 'R' (Recargo).");
            }

            var monto = ParseDecimal(GetChildText(dr, "MontoDescuentooRecargo"));
            if (monto < 0)
            {
                errors.Add($"DescuentoORecargo #{lineNum}: MontoDescuentooRecargo ({monto:F2}) no puede ser negativo.");
            }

            var indicadorDR = GetChildText(dr, "IndicadorFacturacionDescuentooRecargo");
            if (indicadorDR != null)
            {
                var validDR = new HashSet<string> { "1", "2", "3", "4" };
                if (!validDR.Contains(indicadorDR))
                {
                    errors.Add($"DescuentoORecargo #{lineNum}: IndicadorFacturacionDescuentooRecargo '{indicadorDR}' no es válido. Valores permitidos: 1, 2, 3, 4.");
                }
            }
        }
    }
}
