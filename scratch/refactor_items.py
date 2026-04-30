import os

def fix_item_usage(path):
    with open(path, 'r', encoding='utf-8') as f:
        code = f.read()

    # Replacing properties in calculations
    code = code.replace("itm.Quantity * itm.UnitPrice", "itm.CantidadItem * itm.PrecioUnitarioItem")
    code = code.replace("itm.ItbisAmount", "0m") # Simplified as the DTO doesn't have it anymore
    code = code.replace("currentDto.ManualMontoGravadoTotal", "currentDto.ECF.Encabezado.Totales.MontoGravadoTotal")
    code = code.replace("currentDto.ManualMontoExento", "currentDto.ECF.Encabezado.Totales.MontoExento")
    
    # In actualTransmissionTotal calculation
    code = code.replace("itm.ManualDescuentoMonto ?? itm.Discount", "itm.DescuentoMonto ?? 0")
    code = code.replace("itm.ManualRecargoMonto ?? 0", "itm.RecargoMonto ?? 0")
    code = code.replace("itm.IscSpecificAmount + itm.IscAdvaloremAmount + itm.OtherAdditionalTaxAmount", "(itm.IscSpecificAmount ?? 0) + (itm.IscAdvaloremAmount ?? 0) + (itm.OtherAdditionalTaxAmount ?? 0)")

    with open(path, 'w', encoding='utf-8') as f:
        f.write(code)

fix_item_usage("ZynstormECFPlatform.Services/CertificationService.cs")
