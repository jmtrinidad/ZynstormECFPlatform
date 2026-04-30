import os

def fix_dto_usage(path):
    with open(path, 'r', encoding='utf-8') as f:
        code = f.read()

    # Common replacements
    code = code.replace("dto.Ncf", "dto.ECF.Encabezado.IdDoc.eNCF")
    code = code.replace("dto.IssuerRnc", "dto.ECF.Encabezado.Emisor.RNCEmisor")
    code = code.replace("dto.IssueDate", "dto.ECF.Encabezado.Emisor.FechaEmision")
    code = code.replace("dto.Items", "dto.ECF.DetallesItems.Item")
    code = code.replace("dto.ManualMontoTotal", "dto.ECF.Encabezado.Totales.MontoTotal")
    code = code.replace("dto.EcfType", "dto.ECF.Encabezado.IdDoc.TipoeCF")
    
    with open(path, 'w', encoding='utf-8') as f:
        f.write(code)

fix_dto_usage("ZynstormECFPlatform.Services/CertificationService.cs")
fix_dto_usage("ZynstormECFPlatform.Services/EcfGeneratorService.cs")
