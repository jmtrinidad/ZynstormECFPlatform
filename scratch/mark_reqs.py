import json

def process_file():
    with open('ZynstormECFPlatform.Services/requiredByType.json', 'r', encoding='utf-8') as f:
        data = json.load(f)

    # Mapping of requirements by TipoeCF
    requirements_map = {
        31: ["ECF.Encabezado.Comprador.RNCComprador", "ECF.Encabezado.Comprador.RazonSocialComprador"],
        32: ["ECF.Encabezado.Comprador.RNCComprador (Solo si MontoTotal >= 250,000 DOP)"],
        33: ["ECF.InformacionReferencia.NCFModificado", "ECF.InformacionReferencia.FechaNCFModificado", "ECF.InformacionReferencia.CodigoModificacion"],
        34: ["ECF.InformacionReferencia.NCFModificado", "ECF.InformacionReferencia.FechaNCFModificado", "ECF.InformacionReferencia.CodigoModificacion"],
        41: ["ECF.Encabezado.Comprador.RNCComprador", "ECF.Encabezado.Comprador.RazonSocialComprador"],
        43: ["Ninguno adicional al estándar del Emisor"],
        44: ["ECF.Encabezado.Comprador.RNCComprador", "ECF.Encabezado.Comprador.RazonSocialComprador"],
        45: ["ECF.Encabezado.Comprador.RNCComprador", "ECF.Encabezado.Comprador.RazonSocialComprador"],
        46: ["ECF.Encabezado.InformacionesAdicionales (Puerto, Peso, Flete, etc.)", "ECF.Encabezado.Transporte", "ECF.Encabezado.Comprador.IdentificadorExtranjero (o RNC)"],
        47: ["ECF.Encabezado.Comprador.IdentificadorExtranjero", "ECF.Encabezado.Comprador.RazonSocialComprador"]
    }

    for item in data:
        if "ECF" in item and "Encabezado" in item["ECF"] and "IdDoc" in item["ECF"]["Encabezado"]:
            tipo = item["ECF"]["Encabezado"]["IdDoc"].get("TipoeCF")
            if tipo in requirements_map:
                # Insert CamposRequeridos at the top level of this item
                item["CamposRequeridosObligatorios"] = requirements_map[tipo]

    with open('ZynstormECFPlatform.Services/requiredByType.json', 'w', encoding='utf-8') as f:
        json.dump(data, f, indent=2, ensure_ascii=False)

process_file()
