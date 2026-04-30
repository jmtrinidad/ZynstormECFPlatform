import json

def generate_full_md():
    json_path = r"C:\Projects\ZynstormECFPlatform\ZynstormECFPlatform.Services\requiredByType.json"
    md_workspace_path = r"C:\Projects\ZynstormECFPlatform\dgii_ecf_requirements.md"
    
    with open(json_path, 'r', encoding='utf-8') as f:
        examples = json.load(f)

    # Base content
    md_content = """# Especificaciones del DTO ECF - Reglas y Campos Requeridos

Este documento amplía las reglas encontradas en la estructura de facturación electrónica de la DGII, detallando qué nodos son obligatorios por tipo de comprobante y documentando los catálogos principales a utilizar al momento de llenar el DTO `EcfRequest`.

---

## 1. Campos Obligatorios por Tipo de Comprobante (TipoeCF)

Dependiendo del tipo de comprobante (`TipoeCF`) enviado en `ECF.Encabezado.IdDoc.TipoeCF`, la DGII exige la presencia de ciertos campos en el nodo `Comprador` o `InformacionReferencia`.

| TipoeCF | Descripción | Campos Estrictamente Obligatorios (Adicional a los del Emisor) |
| :--- | :--- | :--- |
| **31** | Factura de Crédito Fiscal | `RNCComprador`, `RazonSocialComprador` |
| **32** | Factura de Consumo | *Opcional.* Sólo exige `RNCComprador` o `IdentificadorExtranjero` si el **MontoTotal >= RD$ 250,000**. |
| **33** | Nota de Crédito | Requiere la sección `InformacionReferencia` completa (`NCFModificado`, `FechaNCFModificado`, `CodigoModificacion`). |
| **34** | Nota de Débito | Requiere la sección `InformacionReferencia` completa (`NCFModificado`, `FechaNCFModificado`, `CodigoModificacion`). |
| **41** | Comprobante de Compras | `RNCComprador`, `RazonSocialComprador` |
| **43** | Gastos Menores | *No requiere comprador.* Es para uso interno del emisor. |
| **44** | Regímenes Especiales | `RNCComprador`, `RazonSocialComprador` |
| **45** | Gubernamental | `RNCComprador`, `RazonSocialComprador` |
| **46** | Exportación | Requiere secciones `InformacionesAdicionales`, `Transporte`. Permite `IdentificadorExtranjero` en vez de RNC. Requiere `PaisComprador`. |
| **47** | Pagos al Exterior | Requiere usar `IdentificadorExtranjero` (no RNC) y `RazonSocialComprador`. |

> [!NOTE]
> Todos los documentos asumen que la información del **Emisor** (`RNCEmisor`, `RazonSocialEmisor`, `FechaEmision`) es estrictamente obligatoria. Además, si el `TipoPago` es 2 (Crédito), se debe incluir la `FechaLimitePago`.

---

## 2. Catálogos y Diccionarios de Datos Frecuentes

Al llenar el Payload, los siguientes campos utilizan códigos específicos estandarizados por la DGII.

### Tipo de Pago (`TipoPago`)
Ubicación: `ECF.Encabezado.IdDoc.TipoPago`
| Código | Descripción |
| :---: | :--- |
| **1** | Contado |
| **2** | Crédito (Requiere llenar `FechaLimitePago`) |
| **3** | Gratuito |

### Tipo de Ingreso (`TipoIngresos`)
Ubicación: `ECF.Encabezado.IdDoc.TipoIngresos`
| Código | Descripción |
| :---: | :--- |
| **01** | Ingresos por Operaciones (No financieros) - *El más común* |
| **02** | Ingresos Financieros |
| **03** | Ingresos Extraordinarios |
| **04** | Ingresos por Arrendamientos |
| **05** | Ingresos por Venta de Activo Depreciable |
| **06** | Otros Ingresos |

### Indicador Facturación (Impuestos del Ítem) (`IndicadorFacturacion`)
Ubicación: `ECF.DetallesItems.Item[x].IndicadorFacturacion`
| Código | Descripción |
| :---: | :--- |
| **0** | No Facturable |
| **1** | ITBIS 1 (18%) |
| **2** | ITBIS 2 (16%) |
| **3** | ITBIS 3 (0% - Tasa Cero) |
| **4** | Exento (E) - *Bienes o servicios no gravados* |

### Indicador de Bien o Servicio (`IndicadorBienoServicio`)
Ubicación: `ECF.DetallesItems.Item[x].IndicadorBienoServicio`
| Código | Descripción |
| :---: | :--- |
| **1** | Bien (Artículo físico, mercancía) |
| **2** | Servicio (Intangibles, mano de obra, consultorías) |

### Indicador Envío Diferido (`IndicadorEnvioDiferido`)
Ubicación: `ECF.Encabezado.IdDoc.IndicadorEnvioDiferido`
Indica si el envío a la DGII ocurrirá en un momento posterior a la emisión (offline / asíncrono).
| Código | Descripción |
| :---: | :--- |
| **1** | Sí (El documento se guardará para enviarse en lote después). |
| *No enviar* | Si no se envía el nodo, se asume que el envío es en tiempo real o no diferido. |

---

## 3. Códigos Adicionales Importantes

### Código de Modificación para Notas de Crédito / Débito (`CodigoModificacion`)
Ubicación: `ECF.InformacionReferencia.CodigoModificacion`
| Código | Descripción | Aplica a |
| :---: | :--- | :--- |
| **1** | Anulación total del NCF modificado | Nota de Crédito / Débito |
| **2** | Corrección de texto del comprobante | Nota de Crédito / Débito |
| **3** | Corrección de montos del NCF modificado | Nota de Crédito / Débito |
| **4** | Reemplazo de NCF emitido en contingencia | Condicional |
| **5** | Referencia a Factura de Consumo Electrónica | Nota de Crédito |

### Unidades de Medida Básicas (`UnidadMedida`)
Existen cientos de códigos, pero estos son los más utilizados comercialmente:
| Código | Descripción |
| :---: | :--- |
| **43** | Unidades (UD) |
| **47** | Servicio / Días / Horas |
| **01** | Kilogramos (Kg) |
| **02** | Libras (Lb) |
| **17** | Litros (L) |
| **31** | Galones (Gal) |
| **04** | Metros (M) |

> [!TIP]
> **Integración Rápida**: Si tu sistema no maneja unidades de medida complejas, usa siempre **"43"** para productos tangibles y **"47"** para servicios al momento de mapear tu DTO.

---

## 4. Ejemplos Prácticos de Payload JSON

A continuación se presentan ejemplos básicos y funcionales del objeto JSON a enviar a la plataforma por cada tipo de comprobante principal, ilustrando cómo completar las propiedades descritas anteriormente.
"""

    for item in examples:
        if "ECF" in item and "Encabezado" in item["ECF"] and "IdDoc" in item["ECF"]["Encabezado"]:
            tipo = item["ECF"]["Encabezado"]["IdDoc"].get("TipoeCF")
            notas = item.get("NOTAS", "")
            
            md_content += f"\n### TipoeCF: {tipo}\n"
            if notas:
                md_content += f"> [!NOTE]\n> {notas}\n\n"
                
            if "CamposRequeridosObligatorios" in item:
                del item["CamposRequeridosObligatorios"]
            if "NOTAS" in item:
                del item["NOTAS"]
            if "Notas" in item:
                del item["Notas"]
                
            json_str = json.dumps(item, indent=2, ensure_ascii=False)
            md_content += "```json\n" + json_str + "\n```\n"

    with open(md_workspace_path, 'w', encoding='utf-8') as f:
        f.write(md_content)

if __name__ == '__main__':
    generate_full_md()
