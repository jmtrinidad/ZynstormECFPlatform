import re

def fix_final(path):
    with open(path, 'r', encoding='utf-8') as f:
        code = f.read()

    # Replacements for individualDto
    code = code.replace("individualDto.ManualMontoGravadoI2", "individualDto.ECF.Encabezado.Totales.MontoGravadoI2")
    code = code.replace("individualDto.ManualMontoGravadoI3", "individualDto.ECF.Encabezado.Totales.MontoGravadoI3")
    code = code.replace("individualDto.Items", "individualDto.ECF.DetallesItems.Item")
    code = code.replace("individualDto.CustomerRnc", "individualDto.ECF.Encabezado.Comprador.RNCComprador")
    code = code.replace("individualDto.CustomerName", "individualDto.ECF.Encabezado.Comprador.RazonSocialComprador")
    code = code.replace("individualDto.CustomerAddress", "individualDto.ECF.Encabezado.Comprador.DireccionComprador")
    code = code.replace("individualDto.ReferenceNcf", "individualDto.ECF.InformacionReferencia.NCFModificado")
    code = code.replace("individualDto.ReferenceIssueDate", "individualDto.ECF.InformacionReferencia.FechaNCFModificado")
    
    code = code.replace("individualDto.ECF.InformacionReferencia.CodigoModificacion = 3;", "individualDto.ECF.InformacionReferencia.CodigoModificacion = \"3\";")
    code = code.replace("individualDto.ECF.InformacionReferencia.CodigoModificacion = 2;", "individualDto.ECF.InformacionReferencia.CodigoModificacion = \"2\";")
    code = code.replace("individualDto.ECF.Encabezado.IdDoc.IndicadorNotaCredito = 1;", "individualDto.ECF.Encabezado.IdDoc.IndicadorNotaCredito = \"1\";")
    code = code.replace("individualDto.ECF.Encabezado.IdDoc.IndicadorNotaCredito = 2;", "individualDto.ECF.Encabezado.IdDoc.IndicadorNotaCredito = \"2\";")
    code = code.replace("itm.MontoItem = null;", "itm.MontoItem = 0;")
    
    code = re.sub(r"int ecfType = [^;]+TipoeCF \?\? \(string\.IsNullOrWhiteSpace\([^;]+\.eNCF\) \? 0 : NcfHelper\.ExtractEcfType\([^;]+\.eNCF\)\);", 
                  r"int ecfType = int.Parse(\g<0>".replace("int ecfType = ", "").replace("0 :", "\"0\" :") + ".ToString());", code)

    # I will just blindly replace the exact bad lines with their int.Parse variants:
    code = code.replace("int ecfType = dto.ECF.Encabezado.IdDoc.TipoeCF ?? (string.IsNullOrWhiteSpace(dto.ECF.Encabezado.IdDoc.eNCF) ? 0 : NcfHelper.ExtractEcfType(dto.ECF.Encabezado.IdDoc.eNCF));",
                        "int ecfType = int.Parse(dto.ECF.Encabezado.IdDoc.TipoeCF ?? (string.IsNullOrWhiteSpace(dto.ECF.Encabezado.IdDoc.eNCF) ? \"0\" : NcfHelper.ExtractEcfType(dto.ECF.Encabezado.IdDoc.eNCF).ToString()));")
    code = code.replace("int ecfType = currentDto.ECF.Encabezado.IdDoc.TipoeCF ?? (string.IsNullOrWhiteSpace(currentDto.ECF.Encabezado.IdDoc.eNCF) ? 0 : NcfHelper.ExtractEcfType(currentDto.ECF.Encabezado.IdDoc.eNCF));",
                        "int ecfType = int.Parse(currentDto.ECF.Encabezado.IdDoc.TipoeCF ?? (string.IsNullOrWhiteSpace(currentDto.ECF.Encabezado.IdDoc.eNCF) ? \"0\" : NcfHelper.ExtractEcfType(currentDto.ECF.Encabezado.IdDoc.eNCF).ToString()));")
                        
    code = code.replace("dto.ECF.InformacionReferencia.CodigoModificacion = 3;", "dto.ECF.InformacionReferencia.CodigoModificacion = \"3\";")
    code = code.replace("dto.ECF.InformacionReferencia.CodigoModificacion = 2;", "dto.ECF.InformacionReferencia.CodigoModificacion = \"2\";")

    # Tuple return fixes
    code = code.replace("return (dto.ECF.Encabezado.IdDoc.eNCF, dto.ECF.Encabezado.Emisor.FechaEmision, dto.ECF.Encabezado.Comprador.RNCComprador, dto);",
                        "return (dto.ECF.Encabezado.IdDoc.eNCF, DateTime.ParseExact(dto.ECF.Encabezado.Emisor.FechaEmision ?? DateTime.Now.ToString(\"dd-MM-yyyy\"), \"dd-MM-yyyy\", null), dto.ECF.Encabezado.Comprador.RNCComprador, dto);")

    with open(path, 'w', encoding='utf-8') as f:
        f.write(code)

fix_final("ZynstormECFPlatform.Services/CertificationService.cs")
