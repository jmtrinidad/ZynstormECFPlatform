import re

def fix_final(path):
    with open(path, 'r', encoding='utf-8') as f:
        code = f.read()

    # Replacements
    code = code.replace("ReferenceReasonCode", "ECF.InformacionReferencia.CodigoModificacion")
    code = code.replace("ReferenceCustomerRnc", "ECF.InformacionReferencia.RNCOtroContribuyente")
    code = code.replace("ReferenceReasonDescription", "ECF.InformacionReferencia.RazonModificacion")
    code = code.replace("ManualIndicadorNotaCredito", "ECF.Encabezado.IdDoc.IndicadorNotaCredito")
    
    code = code.replace("UnitPrice", "PrecioUnitarioItem")
    code = code.replace("ManualMontoItem", "MontoItem")

    code = code.replace("int ecfType = currentDto.ECF.Encabezado.IdDoc.TipoeCF ?? (string.IsNullOrWhiteSpace(currentDto.ECF.Encabezado.IdDoc.eNCF) ? 0 : NcfHelper.ExtractEcfType(currentDto.ECF.Encabezado.IdDoc.eNCF));",
                        "int ecfType = int.Parse(currentDto.ECF.Encabezado.IdDoc.TipoeCF ?? (string.IsNullOrWhiteSpace(currentDto.ECF.Encabezado.IdDoc.eNCF) ? \"0\" : NcfHelper.ExtractEcfType(currentDto.ECF.Encabezado.IdDoc.eNCF).ToString()));")

    code = code.replace("int ecfType = dto.ECF.Encabezado.IdDoc.TipoeCF ?? (string.IsNullOrWhiteSpace(dto.ECF.Encabezado.IdDoc.eNCF) ? 0 : NcfHelper.ExtractEcfType(dto.ECF.Encabezado.IdDoc.eNCF));",
                        "int ecfType = int.Parse(dto.ECF.Encabezado.IdDoc.TipoeCF ?? (string.IsNullOrWhiteSpace(dto.ECF.Encabezado.IdDoc.eNCF) ? \"0\" : NcfHelper.ExtractEcfType(dto.ECF.Encabezado.IdDoc.eNCF).ToString()));")

    code = code.replace("currentDto.ECF.Encabezado.IdDoc.TipoPago = 1;", "currentDto.ECF.Encabezado.IdDoc.TipoPago = \"1\";")
    code = code.replace("dto.ECF.Encabezado.IdDoc.TipoPago = 1;", "dto.ECF.Encabezado.IdDoc.TipoPago = \"1\";")
    code = code.replace("dto.ECF.Encabezado.IdDoc.TipoPago = 2;", "dto.ECF.Encabezado.IdDoc.TipoPago = \"2\";")

    # In tuple return:
    # return (dto.ECF.Encabezado.IdDoc.eNCF, dto.ECF.Encabezado.Emisor.FechaEmision, dto.ECF.Encabezado.Comprador.RNCComprador, dto);
    code = code.replace("return (currentDto.ECF.Encabezado.IdDoc.eNCF, currentDto.ECF.Encabezado.Emisor.FechaEmision, currentDto.ECF.Encabezado.Comprador.RNCComprador, currentDto);",
                        "return (currentDto.ECF.Encabezado.IdDoc.eNCF, DateTime.ParseExact(currentDto.ECF.Encabezado.Emisor.FechaEmision ?? DateTime.Now.ToString(\"dd-MM-yyyy\"), \"dd-MM-yyyy\", null), currentDto.ECF.Encabezado.Comprador.RNCComprador, currentDto);")
    code = code.replace("return (dto.ECF.Encabezado.IdDoc.eNCF, dto.ECF.Encabezado.Emisor.FechaEmision, dto.ECF.Encabezado.Comprador.RNCComprador, dto);",
                        "return (dto.ECF.Encabezado.IdDoc.eNCF, DateTime.ParseExact(dto.ECF.Encabezado.Emisor.FechaEmision ?? DateTime.Now.ToString(\"dd-MM-yyyy\"), \"dd-MM-yyyy\", null), dto.ECF.Encabezado.Comprador.RNCComprador, dto);")

    with open(path, 'w', encoding='utf-8') as f:
        f.write(code)

fix_final("ZynstormECFPlatform.Services/CertificationService.cs")
