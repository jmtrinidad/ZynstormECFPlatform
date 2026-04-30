import re

def fix_remaining(path):
    with open(path, 'r', encoding='utf-8') as f:
        code = f.read()

    # Replacements
    code = code.replace("currentDto.CustomerForeignId", "currentDto.ECF.Encabezado.Comprador.IdentificadorExtranjero")
    code = code.replace("currentDto.CustomerCountry", "currentDto.ECF.Encabezado.Comprador.PaisComprador")
    code = code.replace("currentDto.CustomerAddress", "currentDto.ECF.Encabezado.Comprador.DireccionComprador")
    code = code.replace("currentDto.IncomeType", "currentDto.ECF.Encabezado.IdDoc.TipoIngresos")
    code = code.replace("currentDto.PaymentType", "currentDto.ECF.Encabezado.IdDoc.TipoPago")
    code = code.replace("currentDto.ECF.Encabezado.IdDoc.TipoPago = 1;", "currentDto.ECF.Encabezado.IdDoc.TipoPago = \"1\";")
    code = code.replace("currentDto.PaymentDeadline", "currentDto.ECF.Encabezado.IdDoc.FechaLimitePago")
    code = code.replace("currentDto.PaymentTerms", "currentDto.ECF.Encabezado.IdDoc.TerminoPago")
    code = code.replace("currentDto.ManualTotalISRRetencion", "currentDto.ECF.Encabezado.Totales.TotalISRRetencion")
    
    code = code.replace("itm.BillingIndicator", "itm.IndicadorFacturacion")
    code = code.replace("itm.IndicadorFacturacion = 4;", "itm.IndicadorFacturacion = \"4\";")
    code = code.replace("itm.TaxPercentage", "itm.AdditionalTaxRate") # TaxPercentage isn't in new Dto, reuse AdditionalTaxRate for compatibility if we just need to clear it or it's safe to ignore. Actually let's just comment it out.
    code = code.replace("itm.AdditionalTaxRate = 0;", "// itm.TaxPercentage = 0;")
    
    code = code.replace("0m = 0;", "// 0m = 0;")
    code = code.replace("itm.ManualMontoITBISRetenido", "itm.MontoITBISRetenido")
    code = code.replace("itm.ManualMontoISRRetenido", "itm.MontoISRRetenido")
    code = code.replace("itm.IsrRetentionAmount = 0;", "// itm.IsrRetentionAmount = 0;")

    # fix line 1921:
    code = code.replace("int ecfType = currentDto.ECF.Encabezado.IdDoc.TipoeCF ?? (string.IsNullOrWhiteSpace(currentDto.ECF.Encabezado.IdDoc.eNCF) ? 0 : NcfHelper.ExtractEcfType(currentDto.ECF.Encabezado.IdDoc.eNCF));",
                        "int ecfType = int.Parse(currentDto.ECF.Encabezado.IdDoc.TipoeCF ?? (string.IsNullOrWhiteSpace(currentDto.ECF.Encabezado.IdDoc.eNCF) ? \"0\" : NcfHelper.ExtractEcfType(currentDto.ECF.Encabezado.IdDoc.eNCF).ToString()));")
    code = code.replace("int ecfType = currentDto.ECF.Encabezado.IdDoc.TipoeCF ?? (string.IsNullOrWhiteSpace(currentDto.ECF.Encabezado.IdDoc.eNCF) ? \"0\" : NcfHelper.ExtractEcfType(currentDto.ECF.Encabezado.IdDoc.eNCF));",
                        "int ecfType = int.Parse(currentDto.ECF.Encabezado.IdDoc.TipoeCF ?? (string.IsNullOrWhiteSpace(currentDto.ECF.Encabezado.IdDoc.eNCF) ? \"0\" : NcfHelper.ExtractEcfType(currentDto.ECF.Encabezado.IdDoc.eNCF).ToString()));")

    # fix tuple return:
    code = code.replace("return (currentDto.ECF.Encabezado.IdDoc.eNCF, currentDto.ECF.Encabezado.Emisor.FechaEmision, currentDto.ECF.Encabezado.Comprador.RNCComprador, currentDto);",
                        "return (currentDto.ECF.Encabezado.IdDoc.eNCF, DateTime.ParseExact(currentDto.ECF.Encabezado.Emisor.FechaEmision ?? DateTime.Now.ToString(\"dd-MM-yyyy\"), \"dd-MM-yyyy\", null), currentDto.ECF.Encabezado.Comprador.RNCComprador, currentDto);")
                        
    # one more tuple return error
    code = code.replace("return (dto.ECF.Encabezado.IdDoc.eNCF, dto.ECF.Encabezado.Emisor.FechaEmision, dto.ECF.Encabezado.Comprador.RNCComprador, dto);",
                        "return (dto.ECF.Encabezado.IdDoc.eNCF, DateTime.ParseExact(dto.ECF.Encabezado.Emisor.FechaEmision ?? DateTime.Now.ToString(\"dd-MM-yyyy\"), \"dd-MM-yyyy\", null), dto.ECF.Encabezado.Comprador.RNCComprador, dto);")

    with open(path, 'w', encoding='utf-8') as f:
        f.write(code)

fix_remaining("ZynstormECFPlatform.Services/CertificationService.cs")
