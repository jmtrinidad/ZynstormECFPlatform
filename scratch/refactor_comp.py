import re

def fix_code(path):
    with open(path, 'r', encoding='utf-8') as f:
        code = f.read()

    # 1. BillingIndicator -> IndicadorFacturacion
    code = code.replace("it.BillingIndicator == 1", "it.IndicadorFacturacion == \"1\"")
    code = code.replace("it.BillingIndicator == 4", "it.IndicadorFacturacion == \"4\"")
    
    # 2. Quantity * UnitPrice -> CantidadItem * PrecioUnitarioItem
    code = code.replace("it.Quantity * it.UnitPrice", "it.CantidadItem * it.PrecioUnitarioItem")
    
    # 3. ItbisAmount -> 0
    # In MontoTotal calculation: it.CantidadItem * it.PrecioUnitarioItem + it.ItbisAmount
    code = code.replace("+ it.ItbisAmount", "")
    code = code.replace("it => it.ItbisAmount", "it => 0m")
    
    # 4. In CertificationService.cs line 1767:
    # return (currentDto.ECF.Encabezado.IdDoc.eNCF, currentDto.ECF.Encabezado.Emisor.FechaEmision, currentDto.ECF.Encabezado.Comprador.RNCComprador, currentDto);
    # Needs to parse FechaEmision back to DateTime, or we change the tuple type. Let's parse it:
    code = code.replace("return (currentDto.ECF.Encabezado.IdDoc.eNCF, currentDto.ECF.Encabezado.Emisor.FechaEmision, currentDto.ECF.Encabezado.Comprador.RNCComprador, currentDto);",
                        "return (currentDto.ECF.Encabezado.IdDoc.eNCF, DateTime.ParseExact(currentDto.ECF.Encabezado.Emisor.FechaEmision ?? DateTime.Now.ToString(\"dd-MM-yyyy\"), \"dd-MM-yyyy\", null), currentDto.ECF.Encabezado.Comprador.RNCComprador, currentDto);")
                        
    # 5. Line 1921:
    # int ecfType = currentDto.ECF.Encabezado.IdDoc.TipoeCF ?? (string.IsNullOrWhiteSpace(currentDto.ECF.Encabezado.IdDoc.eNCF) ? 0 : NcfHelper.ExtractEcfType(currentDto.ECF.Encabezado.IdDoc.eNCF));
    code = code.replace("int ecfType = currentDto.ECF.Encabezado.IdDoc.TipoeCF ?? (string.IsNullOrWhiteSpace(currentDto.ECF.Encabezado.IdDoc.eNCF) ? \"0\" : NcfHelper.ExtractEcfType(currentDto.ECF.Encabezado.IdDoc.eNCF));",
                        "int ecfType = int.Parse(currentDto.ECF.Encabezado.IdDoc.TipoeCF ?? (string.IsNullOrWhiteSpace(currentDto.ECF.Encabezado.IdDoc.eNCF) ? \"0\" : NcfHelper.ExtractEcfType(currentDto.ECF.Encabezado.IdDoc.eNCF).ToString()));")
    code = code.replace("int ecfType = currentDto.ECF.Encabezado.IdDoc.TipoeCF ?? (string.IsNullOrWhiteSpace(currentDto.ECF.Encabezado.IdDoc.eNCF) ? 0 : NcfHelper.ExtractEcfType(currentDto.ECF.Encabezado.IdDoc.eNCF));",
                        "int ecfType = int.Parse(currentDto.ECF.Encabezado.IdDoc.TipoeCF ?? (string.IsNullOrWhiteSpace(currentDto.ECF.Encabezado.IdDoc.eNCF) ? \"0\" : NcfHelper.ExtractEcfType(currentDto.ECF.Encabezado.IdDoc.eNCF).ToString()));")

    # 6. Lines 1926-1929:
    code = code.replace("itm.IndicadorFacturacion == 4", "itm.IndicadorFacturacion == \"4\"")
    code = code.replace("itm.IndicadorFacturacion == 3", "itm.IndicadorFacturacion == \"3\"")
    code = code.replace("itm.ItbisAmount == 0", "True")
    
    with open(path, 'w', encoding='utf-8') as f:
        f.write(code)

fix_code("ZynstormECFPlatform.Services/CertificationService.cs")
