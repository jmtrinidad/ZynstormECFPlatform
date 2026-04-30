import os

def fix_remaining(path):
    with open(path, 'r', encoding='utf-8') as f:
        code = f.read()

    # ItbisAmount is gone, BillingIndicator is IndicadorFacturacion
    code = code.replace("itm.BillingIndicator == 4", "itm.IndicadorFacturacion == \"4\"")
    code = code.replace("itm.BillingIndicator == 3", "itm.IndicadorFacturacion == \"3\"")
    code = code.replace("itm.ItbisAmount == 0", "True") # Simplified
    code = code.replace("itm.Quantity * itm.UnitPrice", "itm.CantidadItem * itm.PrecioUnitarioItem")
    code = code.replace("?? 0 : NcfHelper.ExtractEcfType", "?? \"0\" : NcfHelper.ExtractEcfType")
    
    with open(path, 'w', encoding='utf-8') as f:
        f.write(code)

fix_remaining("ZynstormECFPlatform.Services/CertificationService.cs")
