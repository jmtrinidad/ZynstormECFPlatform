import os

def fix_dto_usage(path):
    with open(path, 'r', encoding='utf-8') as f:
        code = f.read()

    # Replacing properties for currentDto
    code = code.replace("currentDto.Ncf", "currentDto.ECF.Encabezado.IdDoc.eNCF")
    code = code.replace("currentDto.IssuerRnc", "currentDto.ECF.Encabezado.Emisor.RNCEmisor")
    code = code.replace("currentDto.IssueDate", "currentDto.ECF.Encabezado.Emisor.FechaEmision")
    code = code.replace("currentDto.Items", "currentDto.ECF.DetallesItems.Item")
    code = code.replace("currentDto.ManualMontoTotal", "currentDto.ECF.Encabezado.Totales.MontoTotal")
    code = code.replace("currentDto.ManualTotalITBIS", "currentDto.ECF.Encabezado.Totales.TotalITBIS")
    code = code.replace("currentDto.CustomerRnc", "currentDto.ECF.Encabezado.Comprador.RNCComprador")
    code = code.replace("currentDto.CustomerName", "currentDto.ECF.Encabezado.Comprador.RazonSocialComprador")
    code = code.replace("currentDto.EcfType", "currentDto.ECF.Encabezado.IdDoc.TipoeCF")

    # Replacing properties for indDto
    code = code.replace("indDto.Ncf", "indDto.ECF.Encabezado.IdDoc.eNCF")

    with open(path, 'w', encoding='utf-8') as f:
        f.write(code)

fix_dto_usage("ZynstormECFPlatform.Services/CertificationService.cs")
