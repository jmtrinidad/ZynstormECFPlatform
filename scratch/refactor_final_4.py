import re

def fix_final(path):
    with open(path, 'r', encoding='utf-8') as f:
        code = f.read()

    # Replacements in CertificationService.cs
    code = code.replace("requestDto.ManualMontoGravadoI2", "requestDto.ECF.Encabezado.Totales.MontoGravadoI2")
    code = code.replace("requestDto.ManualMontoGravadoI3", "requestDto.ECF.Encabezado.Totales.MontoGravadoI3")
    code = code.replace("requestDto.Items", "requestDto.ECF.DetallesItems.Item")
    code = code.replace("requestDto.CustomerRnc", "requestDto.ECF.Encabezado.Comprador.RNCComprador")
    code = code.replace("requestDto.CustomerName", "requestDto.ECF.Encabezado.Comprador.RazonSocialComprador")
    code = code.replace("requestDto.CustomerAddress", "requestDto.ECF.Encabezado.Comprador.DireccionComprador")
    code = code.replace("requestDto.ReferenceNcf", "requestDto.ECF.InformacionReferencia.NCFModificado")
    code = code.replace("requestDto.ReferenceIssueDate", "requestDto.ECF.InformacionReferencia.FechaNCFModificado")
    
    code = code.replace("currentDto.ECF.InformacionReferencia.CodigoModificacion = 3;", "currentDto.ECF.InformacionReferencia.CodigoModificacion = \"3\";")
    code = code.replace("currentDto.ECF.InformacionReferencia.CodigoModificacion = 2;", "currentDto.ECF.InformacionReferencia.CodigoModificacion = \"2\";")
    code = code.replace("currentDto.ECF.Encabezado.IdDoc.IndicadorNotaCredito = 1;", "currentDto.ECF.Encabezado.IdDoc.IndicadorNotaCredito = \"1\";")
    code = code.replace("currentDto.ECF.Encabezado.IdDoc.IndicadorNotaCredito = 2;", "currentDto.ECF.Encabezado.IdDoc.IndicadorNotaCredito = \"2\";")
    code = code.replace("itm.MontoItem = null;", "itm.MontoItem = 0;")
    
    code = code.replace("int ecfType = requestDto.ECF.Encabezado.IdDoc.TipoeCF ?? (string.IsNullOrWhiteSpace(requestDto.ECF.Encabezado.IdDoc.eNCF) ? 0 : NcfHelper.ExtractEcfType(requestDto.ECF.Encabezado.IdDoc.eNCF));",
                        "int ecfType = int.Parse(requestDto.ECF.Encabezado.IdDoc.TipoeCF ?? (string.IsNullOrWhiteSpace(requestDto.ECF.Encabezado.IdDoc.eNCF) ? \"0\" : NcfHelper.ExtractEcfType(requestDto.ECF.Encabezado.IdDoc.eNCF).ToString()));")
    code = code.replace("int ecfType = requestDto.EcfType ?? (string.IsNullOrWhiteSpace(requestDto.Ncf) ? 0 : NcfHelper.ExtractEcfType(requestDto.Ncf));",
                        "int ecfType = int.Parse(requestDto.ECF.Encabezado.IdDoc.TipoeCF ?? (string.IsNullOrWhiteSpace(requestDto.ECF.Encabezado.IdDoc.eNCF) ? \"0\" : NcfHelper.ExtractEcfType(requestDto.ECF.Encabezado.IdDoc.eNCF).ToString()));")

    code = code.replace("requestDto.ECF.Encabezado.IdDoc.TipoPago = 1;", "requestDto.ECF.Encabezado.IdDoc.TipoPago = \"1\";")
    code = code.replace("requestDto.ECF.Encabezado.IdDoc.TipoPago = 2;", "requestDto.ECF.Encabezado.IdDoc.TipoPago = \"2\";")

    code = code.replace("return (individualDto.ECF.Encabezado.IdDoc.eNCF, individualDto.ECF.Encabezado.Emisor.FechaEmision, individualDto.ECF.Encabezado.Comprador.RNCComprador, individualDto);",
                        "return (individualDto.ECF.Encabezado.IdDoc.eNCF, DateTime.ParseExact(individualDto.ECF.Encabezado.Emisor.FechaEmision ?? DateTime.Now.ToString(\"dd-MM-yyyy\"), \"dd-MM-yyyy\", null), individualDto.ECF.Encabezado.Comprador.RNCComprador, individualDto);")

    with open(path, 'w', encoding='utf-8') as f:
        f.write(code)

fix_final("ZynstormECFPlatform.Services/CertificationService.cs")
