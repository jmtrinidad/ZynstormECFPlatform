import os

def main():
    path = r"C:\Projects\ZynstormECFPlatform\ZynstormECFPlatform.Dtos\EcfRequestDtos.cs"
    with open(path, 'r', encoding='utf-8') as f:
        code = f.read()

    if "using System.Linq;" not in code:
        code = "using System.Linq;\n" + code

    code = code.replace("public class EcfInvoiceRequestDto\n{", "public class EcfInvoiceRequestDto : IValidatableObject\n{")

    validation_logic = """
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
            case 31: case 41: case 43: case 45:
                if (string.IsNullOrWhiteSpace(e.Comprador?.RNCComprador))
                    yield return new ValidationResult($"Para el comprobante tipo {tipoEcf}, el RNCComprador es obligatorio.", new[] { "ECF.Encabezado.Comprador.RNCComprador" });
                if (string.IsNullOrWhiteSpace(e.Comprador?.RazonSocialComprador))
                    yield return new ValidationResult($"Para el comprobante tipo {tipoEcf}, la RazonSocialComprador es obligatoria.", new[] { "ECF.Encabezado.Comprador.RazonSocialComprador" });
                break;

            case 32:
                if (e.Totales?.MontoTotal >= 250000m && string.IsNullOrWhiteSpace(e.Comprador?.RNCComprador) && string.IsNullOrWhiteSpace(e.Comprador?.IdentificadorExtranjero))
                    yield return new ValidationResult("Para Facturas de Consumo >= 250,000, debe especificar RNCComprador o IdentificadorExtranjero.", new[] { "ECF.Encabezado.Comprador" });
                break;

            case 33: case 34:
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

            case 46: case 47:
                if (string.IsNullOrWhiteSpace(e.Comprador?.IdentificadorExtranjero))
                    yield return new ValidationResult($"Para el comprobante {tipoEcf}, el IdentificadorExtranjero es obligatorio.", new[] { "ECF.Encabezado.Comprador.IdentificadorExtranjero" });
                if (tipoEcf == 46 && string.IsNullOrWhiteSpace(e.Comprador?.PaisComprador))
                    yield return new ValidationResult("Para Exportación (46), el PaisComprador es obligatorio.", new[] { "ECF.Encabezado.Comprador.PaisComprador" });
                break;
        }

        if (ECF?.DetallesItems?.Item == null || !ECF.DetallesItems.Item.Any())
            yield return new ValidationResult("El documento debe contener al menos un ítem.", new[] { "ECF.DetallesItems.Item" });
    }
"""

    code = code.replace("public DateTime? SignatureDateOverride { get; set; }\n}", "public DateTime? SignatureDateOverride { get; set; }\n" + validation_logic + "\n}")

    with open(path, 'w', encoding='utf-8') as f:
        f.write(code)

if __name__ == '__main__':
    main()
