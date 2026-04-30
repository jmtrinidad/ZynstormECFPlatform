# 📋 Especificaciones de Integración DTO ECF
> **Plataforma de Facturación Electrónica (e-CF)**  
> *Guía técnica detallada para la integración de payloads JSON.*

---

## 📑 Tabla de Contenidos
1. [Campos Obligatorios por Tipo de Comprobante](#-1-campos-obligatorios-por-tipo-de-comprobante-tipoecf)
2. [Catálogos y Diccionarios de Datos](#-2-catálogos-y-diccionarios-de-datos)
3. [Códigos Adicionales y Unidades](#-3-códigos-adicionales)
4. [Ejemplos Prácticos de Payload JSON](#-4-ejemplos-prácticos-de-payload-json)

---

## 📌 1. Campos Obligatorios por Tipo de Comprobante (`TipoeCF`)

La siguiente tabla resume los nodos clave y campos que deben estar presentes según el tipo de documento.

| ID | Documento | Nodo Clave | Requerimientos Específicos |
| :---: | :--- | :--- | :--- |
| **31** | 🧾 **Crédito Fiscal** | `Comprador` | 🔹 `RNCComprador`<br>🔹 `RazonSocialComprador` |
| **32** | 🛒 **Consumo** | `Comprador` | ⚠️ Requerido si **MontoTotal ≥ RD$ 250,000** |
| **33** | 📉 **Nota de Crédito** | `Inf. Referencia` | 🔸 `NCFModificado`<br>🔸 `FechaNCFModificado`<br>🔸 `CodigoModificacion` |
| **34** | 📈 **Nota de Débito** | `Inf. Referencia` | 🔸 `NCFModificado`<br>🔸 `FechaNCFModificado`<br>🔸 `CodigoModificacion` |
| **41** | 🛍️ **Compras** | `Comprador` | 🔹 `RNCComprador`<br>🔹 `RazonSocialComprador` |
| **43** | ☕ **Gastos Menores** | *N/A* | ✅ No requiere datos de comprador |
| **44** | 🏢 **Regímenes Esp.** | `Comprador` | 🔹 `RNCComprador`<br>🔹 `RazonSocialComprador` |
| **45** | 🏛️ **Gubernamental** | `Comprador` | 🔹 `RNCComprador`<br>🔹 `RazonSocialComprador` |
| **46** | 🚢 **Exportación** | `Inf. Adicional` | 🌐 `PaisComprador`<br>🌐 `IdentificadorExtranjero` |
| **47** | 🌍 **Pagos Exterior** | `Comprador` | 🌐 `IdentificadorExtranjero`<br>🌐 `RazonSocialComprador` |

> [!IMPORTANT]
> **Campos de Control (Obligatorios Siempre):**  
> `RNCEmisor`, `RazonSocialEmisor` y `FechaEmision` deben estar presentes en el 100% de los envíos.

---

## 📖 2. Catálogos y Diccionarios de Datos

### 💳 Tipo de Pago (`TipoPago`)
*Ubicación: `ECF.Encabezado.IdDoc.TipoPago`*

| Código | Descripción                   | Nota                                |
| :---:  | :---------------------------- | :---------------------------------- |
| **1**  | 💵 **Contado**                | Pago realizado al momento.          |
| **2**  | 💳 **Crédito**                | **Requiere** `FechaLimitePago`.     |
| **3**  | 🎁 **Gratuito**               | Entrega sin valor comercial.        |

### 💰 Tipo de Ingreso (`TipoIngresos`)
*Ubicación: `ECF.Encabezado.IdDoc.TipoIngresos`*

| Código | Descripción                     |
| :---:  | :------------------------------ |
| **01** | Ingresos por Operaciones        |
| **02** | Ingresos Financieros            |
| **03** | Ingresos Extraordinarios        |
| **04** | Ingresos por Arrendamientos     |
| **05** | Venta de Activo Depreciable     |
| **06** | Otros Ingresos                  |

### 📊 Indicador Facturación (`IndicadorFacturacion`)
*Ubicación: `ECF.DetallesItems.Item[x].IndicadorFacturacion`*

| Código | Descripción          | Tasa ITBIS         |
| :---:  | :------------------- | :----------------- |
| **0**  | No Facturable        | N/A                |
| **1**  | ITBIS 1              | **18%** (General)  |
| **2**  | ITBIS 2              | **16%** (Reducido) |
| **3**  | ITBIS 3              | **0%** (Tasa Cero) |
| **4**  | Exento               | Exento de ITBIS    |

---

## 🔑 3. Códigos Adicionales

### 📏 Unidades de Medida (`UnidadMedida`)

| Código | Descripción                             | Categoría Sugerida |
| :---:  | :-------------------------------------- | :----------------- |
| **43** | 📦 **Unidades**                         | Productos          |
| **47** | 🛠️ **Servicios**                        | Servicios          |
| **01** | ⚖️ **Kilogramos**                       | Peso               |
| **17** | 🧪 **Litros**                           | Volumen            |

### 🔄 Códigos de Modificación (`CodigoModificacion`)

| Código | Descripción                        | Uso Típico                 |
| :---:  | :--------------------------------- | :------------------------- |
| **1**  | ❌ **Anulación total**             | Cancelación de factura     |
| **2**  | ✏️ **Corrección de texto**         | Errores en nombres/direc.  |
| **3**  | 💵 **Corrección de montos**        | Ajuste de precios          |
| **5**  | 📑 **Referencia a F. Consumo**     | Notas vinculadas a tipo 32 |

---

## 💻 4. Ejemplos Prácticos de Payload JSON

A continuación se detallan los ejemplos completos para cada tipo de comprobante soportado.

### 🧾 Factura de Crédito Fiscal (TipoeCF: 31)

#### 📄 JSON de Ejemplo
```json
{
  "ECF": {
    "Encabezado": {
      "Version": "1.0",
      "IdDoc": {
        "TipoeCF": 31,
        "eNCF": "E310000000002",
        "FechaVencimientoSecuencia": "31-12-2028",
        "IndicadorEnvioDiferido": 1,
        "TipoIngresos": "01",
        "TipoPago": 1
      },
      "Emisor": {
        "RNCEmisor": "{{RNC}}",
        "RazonSocialEmisor": "{{RAZON_SOCIAL}}",
        "DireccionEmisor": "Av",
        "FechaEmision": "{{FECHA_EMISION}}"
      },
      "Comprador": {
        "RNCComprador": "130862346",
        "RazonSocialComprador": "IT SOLUCLICK SRL"
      },
      "Totales": {
        "MontoExento": 244.0,
        "MontoTotal": 244.0
      }
    },
    "DetallesItems": {
      "Item": [
        {
          "NumeroLinea": 1,
          "IndicadorFacturacion": 4,
          "NombreItem": "CEDEBRAL 5000 JARABE",
          "IndicadorBienoServicio": 1,
          "CantidadItem": 1.0,
          "PrecioUnitarioItem": 244.0,
          "MontoItem": 244.0
        }
      ]
    }
  }
}
```

**Campos Requeridos Específicos:**

**Campos Requeridos Específicos:**

| Campo / Ruta JSON | Importancia | Descripción |
| :--- | :---: | :--- |
| `ECF.Encabezado.Comprador.RNCComprador` | 🔴 **Obligatorio** | ID tributario del receptor. |
| `ECF.Encabezado.Comprador.RazonSocialComprador` | 🔴 **Obligatorio** | Nombre legal del comprador. |

---

### 🛒 Factura de Consumo (TipoeCF: 32)

#### 📄 JSON de Ejemplo
```json
{
  "ECF": {
    "Encabezado": {
      "Version": "1.0",
      "IdDoc": {
        "TipoeCF": 32,
        "eNCF": "E320000000001",
        "IndicadorEnvioDiferido": 1,
        "IndicadorMontoGravado": 0,
        "TipoIngresos": "01",
        "TipoPago": 1
      },
      "Emisor": {
        "RNCEmisor": "{{RNC}}",
        "RazonSocialEmisor": "{{RAZON_SOCIAL}}",
        "DireccionEmisor": "Av",
        "FechaEmision": "{{FECHA_EMISION}}"
      },
      "Comprador": {
        "RNCComprador": "131880657",
        "RazonSocialComprador": "CLIENTES DE LA ADMINISTRACION"
      },
      "Totales": {
        "MontoExento": 601.0,
        "MontoTotal": 601.0
      }
    },
    "DetallesItems": {
      "Item": [
        {
          "NumeroLinea": 1,
          "IndicadorFacturacion": 4,
          "NombreItem": "CORACOR A C/30 TABS.",
          "IndicadorBienoServicio": 1,
          "CantidadItem": 1.0,
          "PrecioUnitarioItem": 601.0,
          "MontoItem": 601.0
        }
      ]
    }
  }
}
```

**Campos Requeridos Específicos:**

| Campo / Ruta JSON | Importancia | Requisito / Nota |
| :--- | :---: | :--- |
| `ECF.Encabezado.Comprador.RNCComprador` | 🟡 **Condicional** | Obligatorio si el **MontoTotal ≥ RD$ 250,000**. |

---

### 🛒 Factura de Consumo (TipoeCF: 32)

> **Descripción:** Factura de consumo por un monto menor a RD$250,000. Este documento se usa como referencia para los tipos 33 y 34.

#### 📄 JSON de Ejemplo
```json
{
  "ECF": {
    "Encabezado": {
      "Version": "1.0",
      "IdDoc": {
        "TipoeCF": 32,
        "eNCF": "E320000000002",
        "IndicadorEnvioDiferido": 1,
        "IndicadorMontoGravado": 0,
        "TipoIngresos": "01",
        "TipoPago": 1
      },
      "Emisor": {
        "RNCEmisor": "{{RNC}}",
        "RazonSocialEmisor": "{{RAZON_SOCIAL}}",
        "DireccionEmisor": "Av",
        "FechaEmision": "{{FECHA_EMISION}}"
      },
      "Comprador": {
        "RNCComprador": "40208719662",
        "RazonSocialComprador": "BRYAN TORRES"
      },
      "Totales": {
        "MontoExento": 600000.0,
        "MontoTotal": 600000.0
      }
    },
    "DetallesItems": {
      "Item": [
        {
          "NumeroLinea": 1,
          "IndicadorFacturacion": 4,
          "NombreItem": "GREEN PIGEON PEAS CARIDOM 24/15 OZ.",
          "IndicadorBienoServicio": 1,
          "CantidadItem": 2.0,
          "PrecioUnitarioItem": 300000.0,
          "MontoItem": 600000.0
        }
      ]
    }
  }
}
```

**Campos Requeridos Específicos:**

| Campo / Ruta JSON | Importancia | Requisito / Nota |
| :--- | :---: | :--- |
| `ECF.Encabezado.Comprador.RNCComprador` | 🟡 **Condicional** | Obligatorio si el **MontoTotal ≥ RD$ 250,000**. |

---

### 📉 Nota de Crédito (TipoeCF: 33)

> **Descripción:** Nota de débito que referencia un comprobante previo. Requiere la sección InformacionReferencia con el eNCF del documento que se modifica.

#### 📄 JSON de Ejemplo
```json
{
  "ECF": {
    "Encabezado": {
      "Version": "1.0",
      "IdDoc": {
        "TipoeCF": 33,
        "eNCF": "E330000000001",
        "FechaVencimientoSecuencia": "31-12-2028",
        "TipoIngresos": "01",
        "TipoPago": 1,
        "TablaFormasPago": {
          "FormaDePago": [
            {
              "FormaPago": 1,
              "MontoPago": "203898.31"
            }
          ]
        }
      },
      "Emisor": {
        "RNCEmisor": "{{RNC}}",
        "RazonSocialEmisor": "{{RAZON_SOCIAL}}",
        "DireccionEmisor": "Av",
        "FechaEmision": "{{FECHA_EMISION}}"
      },
      "Comprador": {
        "RNCComprador": "131880657",
        "RazonSocialComprador": "CLIENTES DE LA ADMINISTRACION",
        "DireccionComprador": "AV. CASANDRA DAMIRON #80"
      },
      "Totales": {
        "MontoExento": "203898.31",
        "MontoTotal": "203898.31"
      }
    },
    "DetallesItems": {
      "Item": [
        {
          "NumeroLinea": 1,
          "IndicadorFacturacion": 4,
          "NombreItem": "GENERAL",
          "IndicadorBienoServicio": 1,
          "CantidadItem": "1",
          "UnidadMedida": "47",
          "PrecioUnitarioItem": "203898.31",
          "MontoItem": "203898.31"
        }
      ]
    },
    "InformacionReferencia": {
      "NCFModificado": "{{NCF_MODIFICADO}}",
      "FechaNCFModificado": "{{FECHA_EMISION}}",
      "CodigoModificacion": "3"
    }
  }
}
```

**Campos Requeridos Específicos:**

**Campos Requeridos Específicos:**

| Campo / Ruta JSON | Importancia | Descripción |
| :--- | :---: | :--- |
| `ECF.InformacionReferencia.NCFModificado` | 🔴 **Obligatorio** | eNCF del documento afectado. |
| `ECF.InformacionReferencia.FechaNCFModificado` | 🔴 **Obligatorio** | Fecha original de emisión. |
| `ECF.InformacionReferencia.CodigoModificacion` | 🔴 **Obligatorio** | Código de motivo (ver catálogo). |

---

### 📈 Nota de Débito (TipoeCF: 34)

> **Descripción:** Nota de crédito que modifica un comprobante emitido previamente. Requiere la sección InformacionReferencia donde NCFModificado es el eNCF de cualquier comprobante que se desee modificar.

#### 📄 JSON de Ejemplo
```json
{
  "ECF": {
    "Encabezado": {
      "Version": "1.0",
      "IdDoc": {
        "TipoeCF": 34,
        "eNCF": "E340000000001",
        "IndicadorNotaCredito": "0",
        "IndicadorEnvioDiferido": 1,
        "IndicadorMontoGravado": 0,
        "TipoIngresos": "01",
        "TipoPago": 2,
        "FechaLimitePago": "{{FECHA_LIMITE_PAGO}}"
      },
      "Emisor": {
        "RNCEmisor": "{{RNC}}",
        "RazonSocialEmisor": "{{RAZON_SOCIAL}}",
        "DireccionEmisor": "Av",
        "FechaEmision": "{{FECHA_EMISION}}"
      },
      "Comprador": {
        "RNCComprador": "131880657",
        "RazonSocialComprador": "CLIENTES DE LA ADMINISTRACION"
      },
      "Totales": {
        "MontoExento": 3005.0,
        "MontoTotal": 3005.0
      }
    },
    "DetallesItems": {
      "Item": [
        {
          "NumeroLinea": 1,
          "IndicadorFacturacion": 4,
          "NombreItem": "CORACOR A C/30 TABS.",
          "IndicadorBienoServicio": 1,
          "CantidadItem": 5.0,
          "PrecioUnitarioItem": 601.0,
          "MontoItem": 3005.0
        }
      ]
    },
    "InformacionReferencia": {
      "NCFModificado": "{{NCF_MODIFICADO}}",
      "FechaNCFModificado": "{{FECHA_EMISION}}",
      "CodigoModificacion": "3"
    }
  }
}
```

**Campos Requeridos Específicos:**

**Campos Requeridos Específicos:**

| Campo / Ruta JSON | Importancia | Descripción |
| :--- | :---: | :--- |
| `ECF.InformacionReferencia.NCFModificado` | 🔴 **Obligatorio** | eNCF del documento afectado. |
| `ECF.InformacionReferencia.FechaNCFModificado` | 🔴 **Obligatorio** | Fecha original de emisión. |
| `ECF.InformacionReferencia.CodigoModificacion` | 🔴 **Obligatorio** | Código de motivo (ver catálogo). |

---

### 🛍️ Comprobante de Compras (TipoeCF: 41)

#### 📄 JSON de Ejemplo
```json
{
  "ECF": {
    "Encabezado": {
      "Version": "1.0",
      "IdDoc": {
        "TipoeCF": 41,
        "eNCF": "E410000000001",
        "FechaVencimientoSecuencia": "31-12-2028",
        "IndicadorMontoGravado": 0,
        "TipoPago": 2,
        "FechaLimitePago": "{{FECHA_LIMITE_PAGO}}"
      },
      "Emisor": {
        "RNCEmisor": "{{RNC}}",
        "RazonSocialEmisor": "{{RAZON_SOCIAL}}",
        "DireccionEmisor": "Av",
        "FechaEmision": "{{FECHA_EMISION}}"
      },
      "Comprador": {
        "RNCComprador": "00100325067",
        "RazonSocialComprador": "ENRIQUE CAMILO SANTOS TAVAREZ"
      },
      "Totales": {
        "MontoGravadoTotal": 1000.0,
        "MontoGravadoI1": 1000.0,
        "ITBIS1": 18,
        "TotalITBIS": 180.0,
        "TotalITBIS1": 180.0,
        "MontoTotal": 1180.0,
        "TotalITBISRetenido": "0.00",
        "TotalISRRetencion": "0.00"
      }
    },
    "DetallesItems": {
      "Item": [
        {
          "NumeroLinea": 1,
          "IndicadorFacturacion": 1,
          "Retencion": {
            "IndicadorAgenteRetencionoPercepcion": 1,
            "MontoITBISRetenido": 0.0
          },
          "NombreItem": "COMISION VERIFON TARJETAS",
          "IndicadorBienoServicio": 1,
          "CantidadItem": 1.0,
          "PrecioUnitarioItem": 1000.0,
          "MontoItem": 1000.0
        }
      ]
    }
  }
}
```

**Campos Requeridos Específicos:**

**Campos Requeridos Específicos:**

| Campo / Ruta JSON | Importancia | Descripción |
| :--- | :---: | :--- |
| `ECF.Encabezado.Comprador.RNCComprador` | 🔴 **Obligatorio** | RNC del emisor original. |
| `ECF.Encabezado.Comprador.RazonSocialComprador` | 🔴 **Obligatorio** | Razón social del emisor original. |

---

### ☕ Gastos Menores (TipoeCF: 43)

#### 📄 JSON de Ejemplo
```json
{
  "ECF": {
    "Encabezado": {
      "Version": "1.0",
      "IdDoc": {
        "TipoeCF": 43,
        "eNCF": "E430000000001",
        "FechaVencimientoSecuencia": "31-12-2028"
      },
      "Emisor": {
        "RNCEmisor": "{{RNC}}",
        "RazonSocialEmisor": "{{RAZON_SOCIAL}}",
        "DireccionEmisor": "Av",
        "FechaEmision": "{{FECHA_EMISION}}"
      },
      "Totales": {
        "MontoExento": 1000.0,
        "MontoTotal": 1000.0
      }
    },
    "DetallesItems": {
      "Item": [
        {
          "NumeroLinea": 1,
          "IndicadorFacturacion": 4,
          "NombreItem": "PROPIETARIO COMPANIA DE TRANSPORTE DIVER",
          "IndicadorBienoServicio": 1,
          "CantidadItem": 1.0,
          "PrecioUnitarioItem": 1000.0,
          "MontoItem": 1000.0
        }
      ]
    }
  }
}
```

**Campos Requeridos Específicos:**

| Requisito | Nota |
| :--- | :--- |
| Estándar de Emisor | No requiere datos adicionales de comprador. |

---

### 🏢 Regímenes Especiales (TipoeCF: 44)

#### 📄 JSON de Ejemplo
```json
{
  "ECF": {
    "Encabezado": {
      "Version": "1.0",
      "IdDoc": {
        "TipoeCF": 44,
        "eNCF": "E440000000001",
        "FechaVencimientoSecuencia": "31-12-2028",
        "IndicadorEnvioDiferido": 1,
        "TipoIngresos": "01",
        "TipoPago": 1
      },
      "Emisor": {
        "RNCEmisor": "{{RNC}}",
        "RazonSocialEmisor": "{{RAZON_SOCIAL}}",
        "DireccionEmisor": "Av",
        "FechaEmision": "{{FECHA_EMISION}}"
      },
      "Comprador": {
        "RNCComprador": "131098843",
        "RazonSocialComprador": "ZONA FRANCA 6 DE NOVIEMBRE SRL"
      },
      "Totales": {
        "MontoExento": 29.5,
        "MontoTotal": 29.5
      }
    },
    "DetallesItems": {
      "Item": [
        {
          "NumeroLinea": 1,
          "IndicadorFacturacion": 4,
          "NombreItem": "GREEN PIGEON PEAS CARIDOM 24/15 OZ.",
          "IndicadorBienoServicio": 1,
          "CantidadItem": 1.0,
          "PrecioUnitarioItem": 29.5,
          "MontoItem": 29.5
        }
      ]
    }
  }
}
```

**Campos Requeridos Específicos:**

**Campos Requeridos Específicos:**

| Campo / Ruta JSON | Importancia | Descripción |
| :--- | :---: | :--- |
| `ECF.Encabezado.Comprador.RNCComprador` | 🔴 **Obligatorio** | RNC del beneficiario del régimen. |
| `ECF.Encabezado.Comprador.RazonSocialComprador` | 🔴 **Obligatorio** | Razón social del beneficiario. |

---

### 🏛️ Gubernamental (TipoeCF: 45)

#### 📄 JSON de Ejemplo
```json
{
  "ECF": {
    "Encabezado": {
      "Version": "1.0",
      "IdDoc": {
        "TipoeCF": 45,
        "eNCF": "E450000000001",
        "FechaVencimientoSecuencia": "31-12-2028",
        "IndicadorEnvioDiferido": 1,
        "TipoIngresos": "01",
        "TipoPago": 1
      },
      "Emisor": {
        "RNCEmisor": "{{RNC}}",
        "RazonSocialEmisor": "{{RAZON_SOCIAL}}",
        "DireccionEmisor": "Av",
        "FechaEmision": "{{FECHA_EMISION}}"
      },
      "Comprador": {
        "RNCComprador": "401506459",
        "RazonSocialComprador": "PLAN DE ASISTENCIA SOCIAL DE LA PRESIDENCIA"
      },
      "Totales": {
        "MontoExento": 1197.0,
        "MontoTotal": 1197.0
      }
    },
    "DetallesItems": {
      "Item": [
        {
          "NumeroLinea": 1,
          "IndicadorFacturacion": 4,
          "NombreItem": "OXIGEN 200 C/30 TABS.",
          "IndicadorBienoServicio": 1,
          "CantidadItem": 1.0,
          "PrecioUnitarioItem": 1197.0,
          "MontoItem": 1197.0
        }
      ]
    }
  }
}
```

**Campos Requeridos Específicos:**

**Campos Requeridos Específicos:**

| Campo / Ruta JSON | Importancia | Descripción |
| :--- | :---: | :--- |
| `ECF.Encabezado.Comprador.RNCComprador` | 🔴 **Obligatorio** | RNC de la institución pública. |
| `ECF.Encabezado.Comprador.RazonSocialComprador` | 🔴 **Obligatorio** | Nombre de la entidad oficial. |

---

### 🚢 Exportación (TipoeCF: 46)

> **Descripción:** Comprobante de exportación. Incluye secciones adicionales de InformacionesAdicionales y Transporte con datos de embarque, aduana y destino.

#### 📄 JSON de Ejemplo
```json
{
  "ECF": {
    "Encabezado": {
      "Version": "1.0",
      "IdDoc": {
        "TipoeCF": 46,
        "eNCF": "E460000000003",
        "FechaVencimientoSecuencia": "31-12-2028",
        "TipoIngresos": "01",
        "TipoPago": 2,
        "FechaLimitePago": "{{FECHA_LIMITE_PAGO}}",
        "TerminoPago": "1 mes",
        "TablaFormasPago": {
          "FormaDePago": [
            {
              "FormaPago": 2,
              "MontoPago": "1800000.00"
            }
          ]
        }
      },
      "Emisor": {
        "RNCEmisor": "{{RNC}}",
        "RazonSocialEmisor": "{{RAZON_SOCIAL}}",
        "DireccionEmisor": "Av",
        "CodigoVendedor": "AA0000000100000000010000000002000000000300000000050000000006",
        "NumeroFacturaInterna": "123456789016",
        "NumeroPedidoInterno": 123456789016,
        "FechaEmision": "{{FECHA_EMISION}}"
      },
      "Comprador": {
        "RNCComprador": "131880681",
        "RazonSocialComprador": "ZONA FRANCA LOI",
        "ContactoComprador": "MARCOS LLUBERES",
        "CorreoComprador": "MARCOSLLUBERES@KKKK.COM",
        "DireccionComprador": "ZONA HAINA",
        "MunicipioComprador": "010100",
        "ProvinciaComprador": "010000",
        "FechaEntrega": "07-04-2020",
        "ContactoEntrega": "JACINTO MANON",
        "DireccionEntrega": "ZONA HAINA",
        "TelefonoAdicional": "809-555-5050",
        "FechaOrdenCompra": "10-03-2020",
        "NumeroOrdenCompra": "4500352230",
        "CodigoInternoComprador": "10633441"
      },
      "InformacionesAdicionales": {
        "FechaEmbarque": "10-04-2020",
        "NumeroEmbarque": "10010-1207-000254",
        "NumeroContenedor": "ERTY227958722",
        "NumeroReferencia": "1448",
        "NombrePuertoEmbarque": "ZONA HAINA",
        "CondicionesEntrega": "FOB",
        "TotalFob": "1800.00",
        "Seguro": "250.00",
        "Flete": "22.00",
        "TotalCif": "2000.00",
        "RegimenAduanero": "EXPORTACION NACIONAL",
        "NombrePuertoSalida": "DOSDQ",
        "NombrePuertoDesembarque": "PTO RICO",
        "PesoBruto": "25000.00",
        "PesoNeto": "24878.00",
        "UnidadPesoBruto": "21",
        "UnidadPesoNeto": "21",
        "CantidadBulto": "250.00",
        "UnidadBulto": "25",
        "VolumenBulto": "45",
        "UnidadVolumen": "27"
      },
      "Transporte": {
        "ViaTransporte": "02",
        "PaisOrigen": "REPUBLICA DOMINICANA",
        "DireccionDestino": "CALLE GUALLUBI NO. 09",
        "PaisDestino": "PUERTO RICO",
        "NumeroAlbaran": "56789UJILLL"
      },
      "Totales": {
        "MontoGravadoTotal": "1800000.00",
        "MontoGravadoI3": "1800000.00",
        "ITBIS3": 0,
        "TotalITBIS": "0.00",
        "TotalITBIS3": "0.00",
        "MontoTotal": "1800000.00"
      }
    },
    "DetallesItems": {
      "Item": [
        {
          "NumeroLinea": 1,
          "TablaCodigosItem": {
            "CodigosItem": [
              {
                "TipoCodigo": "INTERNA",
                "CodigoItem": "123456"
              }
            ]
          },
          "IndicadorFacturacion": 3,
          "NombreItem": "AGUACATE CRIOLLO",
          "IndicadorBienoServicio": 1,
          "CantidadItem": "100.00",
          "UnidadMedida": "43",
          "PrecioUnitarioItem": "18000.00",
          "MontoItem": "1800000.00"
        }
      ]
    }
  }
}
```

**Campos Requeridos Específicos:**

**Campos Requeridos Específicos:**

| Campo / Ruta JSON | Importancia | Descripción |
| :--- | :---: | :--- |
| `ECF.Encabezado.InformacionesAdicionales` | 🔴 **Obligatorio** | Datos de Puerto, Peso, Flete, etc. |
| `ECF.Encabezado.Transporte` | 🔴 **Obligatorio** | Detalles del medio de transporte. |
| `ECF.Encabezado.Comprador.IdentificadorExtranjero` | 🔴 **Obligatorio** | ID fiscal internacional. |

---

### 🌍 Pagos al Exterior (TipoeCF: 47)

> **Descripción:** Comprobante para pagos a proveedores en el exterior. Incluye secciones de OtraMoneda y Subtotales, y usa IdentificadorExtranjero en lugar de RNC del comprador.

#### 📄 JSON de Ejemplo
```json
{
  "ECF": {
    "Encabezado": {
      "Version": "1.0",
      "IdDoc": {
        "TipoeCF": 47,
        "eNCF": "E470000000001",
        "FechaVencimientoSecuencia": "31-12-2028",
        "NumeroCuentaPago": "BB00058745214789635111111111",
        "BancoPago": "BB0111111111111111111111111111111111111111111111111111111111111111111111111"
      },
      "Emisor": {
        "RNCEmisor": "{{RNC}}",
        "RazonSocialEmisor": "{{RAZON_SOCIAL}}",
        "DireccionEmisor": "Av",
        "NumeroFacturaInterna": "123456789016",
        "NumeroPedidoInterno": 123456789016,
        "FechaEmision": "{{FECHA_EMISION}}"
      },
      "Comprador": {
        "IdentificadorExtranjero": "533445888",
        "RazonSocialComprador": "ALEJA FERMIN SANTOS"
      },
      "Totales": {
        "MontoExento": "180000.00",
        "MontoTotal": "180000.00",
        "TotalISRRetencion": "48600.00"
      },
      "OtraMoneda": {
        "TipoMoneda": "USD",
        "TipoCambio": "60.0000",
        "MontoExentoOtraMoneda": "3000.00",
        "MontoTotalOtraMoneda": "3000.00"
      }
    },
    "DetallesItems": {
      "Item": [
        {
          "NumeroLinea": 1,
          "IndicadorFacturacion": 4,
          "Retencion": {
            "IndicadorAgenteRetencionoPercepcion": "1",
            "MontoISRRetenido": "48600.00"
          },
          "NombreItem": "LICENCIA WYI",
          "IndicadorBienoServicio": 2,
          "CantidadItem": "1.00",
          "UnidadMedida": "43",
          "PrecioUnitarioItem": "180000.00",
          "OtraMonedaDetalle": {
            "PrecioOtraMoneda": "3000.0000",
            "MontoItemOtraMoneda": "3000.00"
          },
          "MontoItem": "180000.00"
        }
      ]
    },
    "Subtotales": {
      "Subtotal": [
        {
          "NumeroSubTotal": "1",
          "DescripcionSubtotal": "N/A",
          "Orden": 1,
          "SubTotalExento": "180000.00",
          "MontoSubTotal": "180000.00",
          "Lineas": 1
        }
      ]
    }
  }
}
```

**Campos Requeridos Específicos:**

**Campos Requeridos Específicos:**

| Campo / Ruta JSON | Importancia | Descripción |
| :--- | :---: | :--- |
| `ECF.Encabezado.Comprador.IdentificadorExtranjero` | 🔴 **Obligatorio** | ID fiscal en el país origen. |
| `ECF.Encabezado.Comprador.RazonSocialComprador` | 🔴 **Obligatorio** | Razón social internacional. |

---

