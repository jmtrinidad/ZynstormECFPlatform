import json

def generate_beautiful_md():
    json_path = r"C:\Projects\ZynstormECFPlatform\ZynstormECFPlatform.Services\requiredByType.json"
    md_workspace_path = r"C:\Projects\ZynstormECFPlatform\dgii_ecf_requirements.md"
    
    with open(json_path, 'r', encoding='utf-8') as f:
        examples = json.load(f)

    # Base content with emojis and better spacing
    md_content = """# 📋 Especificaciones de Integración DTO ECF - Reglas y Campos Requeridos

Este documento es una guía detallada para integrar su sistema con la plataforma de Facturación Electrónica. Incluye los campos obligatorios por tipo de comprobante, catálogos de códigos y ejemplos de payload JSON.

---

## 📌 1. Campos Obligatorios por Tipo de Comprobante (TipoeCF)

Dependiendo del `TipoeCF`, se deben completar nodos específicos como `Comprador` o `InformacionReferencia`.

| TipoeCF | Descripción | Nodo Requerido | Campos Específicos |
| :--- | :--- | :--- | :--- |
| **31** | 🧾 Crédito Fiscal | `Comprador` | `RNCComprador`, `RazonSocialComprador` |
| **32** | 🛒 Consumo | `Comprador` | Requerido si **MontoTotal >= RD$ 250,000** |
| **33** | 📉 Nota de Crédito | `Inf. Referencia` | `NCFModificado`, `FechaNCFModificado`, `CodigoModificacion` |
| **34** | 📈 Nota de Débito | `Inf. Referencia` | `NCFModificado`, `FechaNCFModificado`, `CodigoModificacion` |
| **41** | 🛍️ Compras | `Comprador` | `RNCComprador`, `RazonSocialComprador` |
| **43** | ☕ Gastos Menores | *N/A* | No requiere datos de comprador |
| **44** | 🏢 Regímenes Esp. | `Comprador` | `RNCComprador`, `RazonSocialComprador` |
| **45** | 🏛️ Gubernamental | `Comprador` | `RNCComprador`, `RazonSocialComprador` |
| **46** | 🚢 Exportación | `Inf. Adicional` | `PaisComprador`, `IdentificadorExtranjero` |
| **47** | 🌍 Pagos Exterior | `Comprador` | `IdentificadorExtranjero`, `RazonSocialComprador` |

> [!IMPORTANT]
> **Datos del Emisor:** `RNCEmisor`, `RazonSocialEmisor` y `FechaEmision` son obligatorios en todos los casos.

---

## 📖 2. Catálogos y Diccionarios de Datos

### 💳 Tipo de Pago (`TipoPago`)
*Ubicación: `ECF.Encabezado.IdDoc.TipoPago`*

| Código | Descripción | Nota |
| :--- | :--- | :--- |
| **1** | Contado | Pago al momento |
| **2** | Crédito | **Requiere** `FechaLimitePago` |
| **3** | Gratuito | Sin valor comercial |

### 💰 Tipo de Ingreso (`TipoIngresos`)
*Ubicación: `ECF.Encabezado.IdDoc.TipoIngresos`*

| Código | Descripción |
| :--- | :--- |
| **01** | Ingresos por Operaciones |
| **02** | Ingresos Financieros |
| **03** | Ingresos Extraordinarios |
| **04** | Ingresos por Arrendamientos |
| **05** | Venta de Activo Depreciable |
| **06** | Otros Ingresos |

### 📊 Indicador Facturación (`IndicadorFacturacion`)
*Ubicación: `ECF.DetallesItems.Item[x].IndicadorFacturacion`*

| Código | Descripción | Tasa ITBIS |
| :--- | :--- | :--- |
| **0** | No Facturable | N/A |
| **1** | ITBIS 1 | **18%** |
| **2** | ITBIS 2 | **16%** |
| **3** | ITBIS 3 | **0%** (Tasa Cero) |
| **4** | Exento | Exento de ITBIS |

---

## 🔑 3. Códigos Adicionales

### 📏 Unidades de Medida (`UnidadMedida`)

| Código | Descripción |
| :--- | :--- |
| **43** | Unidades (Predeterminado productos) |
| **47** | Servicios (Predeterminado servicios) |
| **01** | Kilogramos |
| **17** | Litros |

### 🔄 Códigos de Modificación (`CodigoModificacion`)

| Código | Descripción |
| :--- | :--- |
| **1** | Anulación total |
| **2** | Corrección de texto |
| **3** | Corrección de montos |
| **5** | Referencia a Factura de Consumo |

---

## 💻 4. Ejemplos Prácticos de Payload JSON

A continuación se detallan los ejemplos completos para cada tipo de comprobante soportado.

"""

    tipo_map = {
        31: "🧾 Factura de Crédito Fiscal",
        32: "🛒 Factura de Consumo",
        33: "📉 Nota de Crédito",
        34: "📈 Nota de Débito",
        41: "🛍️ Comprobante de Compras",
        43: "☕ Gastos Menores",
        44: "🏢 Regímenes Especiales",
        45: "🏛️ Gubernamental",
        46: "🚢 Exportación",
        47: "🌍 Pagos al Exterior"
    }

    for item in examples:
        if "ECF" in item and "Encabezado" in item["ECF"] and "IdDoc" in item["ECF"]["Encabezado"]:
            tipo = item["ECF"]["Encabezado"]["IdDoc"].get("TipoeCF")
            notas = item.get("NOTAS") or (item.get("Notas") if isinstance(item.get("Notas"), str) else item.get("Notas", {}).get("NOTAS"))
            
            title_desc = tipo_map.get(int(tipo), "Documento")
            
            md_content += f"### {title_desc} (TipoeCF: {tipo})\n\n"
            if notas:
                md_content += f"> **Descripción:** {notas}\n\n"
            
            # Clean up the JSON for display
            display_item = {k: v for k, v in item.items() if k not in ["CamposRequeridosObligatorios", "NOTAS", "Notas"]}
                
            json_str = json.dumps(display_item, indent=2, ensure_ascii=False)
            
            md_content += "#### 📄 JSON de Ejemplo\n"
            md_content += "```json\n" + json_str + "\n```\n\n"
            
            if "CamposRequeridosObligatorios" in item:
                md_content += "**Campos Requeridos Específicos:**\n"
                for campo in item["CamposRequeridosObligatorios"]:
                    md_content += f"- `{campo}`\n"
                md_content += "\n"
                
            md_content += "---\n\n"

    with open(md_workspace_path, 'w', encoding='utf-8') as f:
        f.write(md_content)

if __name__ == '__main__':
    generate_beautiful_md()
