# Guia de llenado: EcfInvoiceRequestDto

Esta guia aplica al objeto nuevo que recibe `EmitEcf` en produccion. Todos los tipos de comprobante usan la misma raiz:

```json
{
  "externalReference": "INV-2026-0001",
  "ECF": {
    "Encabezado": {
      "Version": "1.0",
      "IdDoc": {},
      "Emisor": {},
      "Comprador": {},
      "Totales": {}
    },
    "DetallesItems": {
      "Item": []
    },
    "DescuentosORecargos": null,
    "InformacionReferencia": null
  }
}
```

## Campos comunes obligatorios

| Campo | Regla |
| --- | --- |
| `ECF.Encabezado.IdDoc.TipoeCF` | Tipo de comprobante: `31`, `32`, `33`, `34`, `41`, `43`, `44`, `45`, `46` o `47`. |
| `ECF.Encabezado.IdDoc.eNCF` | Debe tener formato `E` + tipo de 2 digitos + secuencia de 10 digitos. Ejemplo: `E310000000001`. |
| `ECF.Encabezado.IdDoc.TipoIngresos` | Obligatorio para tipos `31`, `32`, `33`, `34`, `44`, `45` y `46`. No aplica para `41`, `43` y `47`. |
| `ECF.Encabezado.IdDoc.TipoPago` | Use `1` contado o `2` credito. |
| `ECF.Encabezado.IdDoc.FechaLimitePago` | Obligatorio cuando `TipoPago` es `2`. Formato recomendado: `dd-MM-yyyy`. |
| `ECF.Encabezado.Emisor.RNCEmisor` | RNC del emisor, solo digitos. |
| `ECF.Encabezado.Emisor.RazonSocialEmisor` | Nombre legal del emisor. |
| `ECF.Encabezado.Emisor.DireccionEmisor` | Direccion fiscal del emisor. |
| `ECF.Encabezado.Emisor.FechaEmision` | Fecha del comprobante. Formato recomendado: `dd-MM-yyyy`. |
| `ECF.Encabezado.Totales.MontoTotal` | Total del comprobante. |
| `ECF.DetallesItems.Item` | Debe contener al menos un item. |

Cada item debe traer:

| Campo | Regla |
| --- | --- |
| `NumeroLinea` | Numero de linea como texto o numero serializado. Ejemplo: `1`. |
| `IndicadorFacturacion` | Indicador DGII. Ejemplo: `1` para gravado ITBIS 18%, `4` para exento. |
| `NombreItem` | Nombre del producto o servicio. |
| `CantidadItem` | Mayor que cero. |
| `PrecioUnitarioItem` | Cero o mayor. |
| `MontoItem` | Mayor que cero. Debe representar el monto neto de la linea luego de aplicar descuentos o recargos. |
| `AdditionalTaxRate` | Obligatorio solo si se envia `IscType`. |

## Productos exentos

Cuando el producto o servicio es exento:

| Campo | Valor esperado |
| --- | --- |
| `Item[].IndicadorFacturacion` | `4`. |
| `ECF.Encabezado.Totales.MontoExento` | Suma de los `MontoItem` exentos. |
| `ECF.Encabezado.Totales.MontoGravadoTotal` | No incluya los items exentos en este total. |
| `ECF.Encabezado.Totales.TotalITBIS` | No debe sumar ITBIS por items exentos. |
| `ECF.Encabezado.Totales.MontoTotal` | Total final incluyendo gravados, exentos, impuestos, descuentos y recargos. |

Ejemplo de item exento:

```json
{
  "NumeroLinea": "1",
  "IndicadorFacturacion": "4",
  "NombreItem": "Producto exento",
  "IndicadorBienoServicio": "1",
  "CantidadItem": 2,
  "PrecioUnitarioItem": 500.00,
  "MontoItem": 1000.00
}
```

## Descuentos por item

Los descuentos de linea se informan dentro del item. El `MontoItem` debe quedar neto, es decir:

```text
MontoItem = (CantidadItem * PrecioUnitarioItem) - DescuentoMonto + RecargoMonto
```

Si el descuento es simple, puede enviar solo `DescuentoMonto`:

```json
{
  "NumeroLinea": "1",
  "IndicadorFacturacion": "1",
  "NombreItem": "Servicio con descuento",
  "IndicadorBienoServicio": "2",
  "CantidadItem": 1,
  "PrecioUnitarioItem": 1000.00,
  "DescuentoMonto": 100.00,
  "MontoItem": 900.00
}
```

Si necesita detallar el descuento, use `TablaSubDescuento.SubDescuento`. La suma de los subdescuentos debe coincidir con `DescuentoMonto`.

```json
{
  "NumeroLinea": "1",
  "IndicadorFacturacion": "1",
  "NombreItem": "Servicio con subdescuento",
  "IndicadorBienoServicio": "2",
  "CantidadItem": 1,
  "PrecioUnitarioItem": 1000.00,
  "DescuentoMonto": 100.00,
  "TablaSubDescuento": {
    "SubDescuento": [
      {
        "TipoSubDescuento": "$",
        "MontoSubDescuento": 100.00
      }
    ]
  },
  "MontoItem": 900.00
}
```

En los totales, refleje el descuento en el monto gravado o exento segun el indicador del item. Por ejemplo, una linea gravada de `1000.00` con descuento de `100.00` queda con base gravada `900.00`; si aplica ITBIS 18%, el `TotalITBIS` seria `162.00` y el `MontoTotal` seria `1062.00`.

## Descuento global o recargo global

Cuando el descuento no pertenece a una linea especifica del item, use el nodo `ECF.DescuentosORecargos`. Este nodo se genera en el XML como `DescuentosORecargos/DescuentoORecargo`, igual que en los ejemplos de `ZynstormECFPlatform.Schemas/XmlProd`, por ejemplo `Paso_1_E310000000140.xml`.

| Campo | Regla |
| --- | --- |
| `NumeroLinea` | Numero secuencial del ajuste global. En los XML de referencia se usa `1`. |
| `TipoAjuste` | `D` para descuento o `R` para recargo. |
| `DescripcionDescuentooRecargo` | Descripcion corta del descuento o recargo. |
| `TipoValor` | `$` cuando el valor es monto fijo, `%` cuando es porcentaje. |
| `ValorDescuentooRecargo` | Valor del descuento o recargo. En monto fijo normalmente coincide con `MontoDescuentooRecargo`. |
| `MontoDescuentooRecargo` | Monto real aplicado. No puede ser negativo. |
| `IndicadorFacturacionDescuentooRecargo` | Indicador al que aplica el ajuste: `1`, `2`, `3` o `4`. Use `4` si el ajuste aplica a monto exento. |

Ejemplo de descuento global de RD$75.05 sobre monto gravado ITBIS 18%:

```json
{
  "DescuentosORecargos": {
    "DescuentoORecargo": [
      {
        "NumeroLinea": "1",
        "TipoAjuste": "D",
        "DescripcionDescuentooRecargo": "Descuento por certificacion",
        "TipoValor": "$",
        "ValorDescuentooRecargo": 75.05,
        "MontoDescuentooRecargo": 75.05,
        "IndicadorFacturacionDescuentooRecargo": "1"
      }
    ]
  }
}
```

En un descuento global, los `Item[].MontoItem` pueden quedar con su valor bruto original y el ajuste se informa en `DescuentosORecargos`. Los totales deben cuadrar con el resultado final despues del descuento o recargo global. Por ejemplo, si las lineas suman `2302.00` gravadas y se aplica un descuento global de `75.05`, el `MontoGravadoTotal` debe quedar en `2226.95`, el `TotalITBIS` en `400.85` y el `MontoTotal` en `2627.80`.

No mezcle el mismo descuento en ambos lugares. Use `DescuentoMonto` cuando el descuento pertenece a una linea. Use `DescuentosORecargos` cuando el descuento aplica al comprobante completo o no se quiere asociar al item.

## Reglas por tipo

| Tipo | Nombre comun | Campos adicionales |
| --- | --- | --- |
| `31` | Factura de credito fiscal | `Comprador.RNCComprador` y `Comprador.RazonSocialComprador`. |
| `32` | Factura de consumo | Si `MontoTotal >= 250000`, enviar `Comprador.RNCComprador` o `Comprador.IdentificadorExtranjero`. Si es menor, el comprador puede ir vacio. |
| `33` | Nota de debito | `InformacionReferencia.NCFModificado`, `FechaNCFModificado` y `CodigoModificacion`. |
| `34` | Nota de credito | `InformacionReferencia.NCFModificado`, `FechaNCFModificado` y `CodigoModificacion`. |
| `41` | Compras | `Comprador.RNCComprador` y `Comprador.RazonSocialComprador`. |
| `43` | Gastos menores | No requiere datos del comprador. |
| `44` | Regimenes especiales | `Comprador.RNCComprador` y `Comprador.RazonSocialComprador`. |
| `45` | Gubernamental | `Comprador.RNCComprador` y `Comprador.RazonSocialComprador`. |
| `46` | Exportacion | `Comprador.RazonSocialComprador`, `Comprador.PaisComprador`, y `Comprador.IdentificadorExtranjero` o `Comprador.RNCComprador`. |
| `47` | Pagos al exterior | `Comprador.IdentificadorExtranjero` y `Comprador.RazonSocialComprador`. |

Para `46`, el validador XML tambien puede exigir nodos propios de exportacion segun el XSD DGII. El DTO actual cubre los datos del comprador, pero si el caso productivo necesita transporte/exportacion extendida hay que agregar esos campos al contrato antes de enviarlos a DGII.

## Ejemplo base tipo 31

```json
{
  "externalReference": "INV-31-0001",
  "ECF": {
    "Encabezado": {
      "Version": "1.0",
      "IdDoc": {
        "TipoeCF": "31",
        "eNCF": "E310000000001",
        "FechaVencimientoSecuencia": "31-12-2028",
        "TipoIngresos": "01",
        "TipoPago": "1"
      },
      "Emisor": {
        "RNCEmisor": "131880681",
        "RazonSocialEmisor": "ZYNSTORM SRL",
        "DireccionEmisor": "Av. Principal 1",
        "FechaEmision": "15-05-2026"
      },
      "Comprador": {
        "RNCComprador": "130862346",
        "RazonSocialComprador": "CLIENTE SRL"
      },
      "Totales": {
        "MontoGravadoTotal": 1000.00,
        "ITBIS1": 18,
        "TotalITBIS": 180.00,
        "MontoTotal": 1180.00
      }
    },
    "DetallesItems": {
      "Item": [
        {
          "NumeroLinea": "1",
          "IndicadorFacturacion": "1",
          "NombreItem": "Servicio profesional",
          "IndicadorBienoServicio": "2",
          "CantidadItem": 1,
          "PrecioUnitarioItem": 1000.00,
          "MontoItem": 1000.00
        }
      ]
    }
  }
}
```

## Ejemplos por tipo

### Tipo 32 menor a RD$250,000

```json
{
  "externalReference": "INV-32-0001",
  "ECF": {
    "Encabezado": {
      "Version": "1.0",
      "IdDoc": { "TipoeCF": "32", "eNCF": "E320000000001", "TipoIngresos": "01", "TipoPago": "1" },
      "Emisor": { "RNCEmisor": "131880681", "RazonSocialEmisor": "ZYNSTORM SRL", "DireccionEmisor": "Av. Principal 1", "FechaEmision": "15-05-2026" },
      "Comprador": {},
      "Totales": { "MontoExento": 1500.00, "MontoTotal": 1500.00 }
    },
    "DetallesItems": { "Item": [{ "NumeroLinea": "1", "IndicadorFacturacion": "4", "NombreItem": "Producto exento", "CantidadItem": 1, "PrecioUnitarioItem": 1500.00, "MontoItem": 1500.00 }] }
  }
}
```

### Tipo 32 con producto exento y descuento

```json
{
  "externalReference": "INV-32-DESC-EXENTO",
  "ECF": {
    "Encabezado": {
      "Version": "1.0",
      "IdDoc": { "TipoeCF": "32", "eNCF": "E320000000003", "TipoIngresos": "01", "TipoPago": "1" },
      "Emisor": { "RNCEmisor": "131880681", "RazonSocialEmisor": "ZYNSTORM SRL", "DireccionEmisor": "Av. Principal 1", "FechaEmision": "15-05-2026" },
      "Comprador": {},
      "Totales": {
        "MontoExento": 900.00,
        "MontoTotal": 900.00
      }
    },
    "DetallesItems": {
      "Item": [
        {
          "NumeroLinea": "1",
          "IndicadorFacturacion": "4",
          "NombreItem": "Producto exento con descuento",
          "IndicadorBienoServicio": "1",
          "CantidadItem": 1,
          "PrecioUnitarioItem": 1000.00,
          "DescuentoMonto": 100.00,
          "TablaSubDescuento": {
            "SubDescuento": [
              { "TipoSubDescuento": "$", "MontoSubDescuento": 100.00 }
            ]
          },
          "MontoItem": 900.00
        }
      ]
    }
  }
}
```

### Tipo 31 con descuento global

```json
{
  "externalReference": "INV-31-DESC-GLOBAL",
  "ECF": {
    "Encabezado": {
      "Version": "1.0",
      "IdDoc": {
        "TipoeCF": "31",
        "eNCF": "E310000000140",
        "FechaVencimientoSecuencia": "31-12-2028",
        "IndicadorMontoGravado": "0",
        "TipoIngresos": "01",
        "TipoPago": "2",
        "FechaLimitePago": "07-06-2026",
        "TerminoPago": "30 DIAS"
      },
      "Emisor": {
        "RNCEmisor": "132878191",
        "RazonSocialEmisor": "CESAR Y JULIO AUTO SERVICIOS SRL",
        "DireccionEmisor": "CALLE PRINCIPAL #1",
        "FechaEmision": "12-05-2026"
      },
      "Comprador": {
        "RNCComprador": "133009889",
        "RazonSocialComprador": "TRANSPORTE NJ SRL",
        "DireccionComprador": "AV. CASANDRA DAMIRON #80"
      },
      "Totales": {
        "MontoGravadoTotal": 2226.95,
        "MontoGravadoI1": 2226.95,
        "ITBIS1": 18,
        "TotalITBIS": 400.85,
        "TotalITBIS1": 400.85,
        "MontoTotal": 2627.80
      }
    },
    "DetallesItems": {
      "Item": [
        {
          "NumeroLinea": "1",
          "IndicadorFacturacion": "1",
          "NombreItem": "Cambio de Aceite (Labor)",
          "IndicadorBienoServicio": "1",
          "CantidadItem": 1,
          "UnidadMedida": "43",
          "PrecioUnitarioItem": 1502.00,
          "MontoItem": 1502.00
        },
        {
          "NumeroLinea": "2",
          "IndicadorFacturacion": "1",
          "NombreItem": "Revision de Frenos",
          "IndicadorBienoServicio": "1",
          "CantidadItem": 1,
          "UnidadMedida": "43",
          "PrecioUnitarioItem": 800.00,
          "MontoItem": 800.00
        }
      ]
    },
    "DescuentosORecargos": {
      "DescuentoORecargo": [
        {
          "NumeroLinea": "1",
          "TipoAjuste": "D",
          "DescripcionDescuentooRecargo": "Descuento por certificacion",
          "TipoValor": "$",
          "ValorDescuentooRecargo": 75.05,
          "MontoDescuentooRecargo": 75.05,
          "IndicadorFacturacionDescuentooRecargo": "1"
        }
      ]
    }
  }
}
```

### Tipo 32 igual o mayor a RD$250,000

```json
{
  "externalReference": "INV-32-250K",
  "ECF": {
    "Encabezado": {
      "Version": "1.0",
      "IdDoc": { "TipoeCF": "32", "eNCF": "E320000000002", "TipoIngresos": "01", "TipoPago": "1" },
      "Emisor": { "RNCEmisor": "131880681", "RazonSocialEmisor": "ZYNSTORM SRL", "DireccionEmisor": "Av. Principal 1", "FechaEmision": "15-05-2026" },
      "Comprador": { "RNCComprador": "00112345678", "RazonSocialComprador": "JUAN PEREZ" },
      "Totales": { "MontoExento": 250000.00, "MontoTotal": 250000.00 }
    },
    "DetallesItems": { "Item": [{ "NumeroLinea": "1", "IndicadorFacturacion": "4", "NombreItem": "Equipo", "CantidadItem": 1, "PrecioUnitarioItem": 250000.00, "MontoItem": 250000.00 }] }
  }
}
```

### Tipo 33 nota de debito

```json
{
  "externalReference": "ND-33-0001",
  "ECF": {
    "Encabezado": {
      "Version": "1.0",
      "IdDoc": { "TipoeCF": "33", "eNCF": "E330000000001", "TipoIngresos": "01", "TipoPago": "1" },
      "Emisor": { "RNCEmisor": "131880681", "RazonSocialEmisor": "ZYNSTORM SRL", "DireccionEmisor": "Av. Principal 1", "FechaEmision": "15-05-2026" },
      "Comprador": { "RNCComprador": "130862346", "RazonSocialComprador": "CLIENTE SRL" },
      "Totales": { "MontoExento": 500.00, "MontoTotal": 500.00 }
    },
    "DetallesItems": { "Item": [{ "NumeroLinea": "1", "IndicadorFacturacion": "4", "NombreItem": "Ajuste por diferencia", "CantidadItem": 1, "PrecioUnitarioItem": 500.00, "MontoItem": 500.00 }] },
    "InformacionReferencia": { "NCFModificado": "E310000000001", "FechaNCFModificado": "14-05-2026", "CodigoModificacion": "3" }
  }
}
```

### Tipo 34 nota de credito

```json
{
  "externalReference": "NC-34-0001",
  "ECF": {
    "Encabezado": {
      "Version": "1.0",
      "IdDoc": { "TipoeCF": "34", "eNCF": "E340000000001", "TipoIngresos": "01", "TipoPago": "1" },
      "Emisor": { "RNCEmisor": "131880681", "RazonSocialEmisor": "ZYNSTORM SRL", "DireccionEmisor": "Av. Principal 1", "FechaEmision": "15-05-2026" },
      "Comprador": { "RNCComprador": "130862346", "RazonSocialComprador": "CLIENTE SRL" },
      "Totales": { "MontoExento": 250.00, "MontoTotal": 250.00 }
    },
    "DetallesItems": { "Item": [{ "NumeroLinea": "1", "IndicadorFacturacion": "4", "NombreItem": "Devolucion parcial", "CantidadItem": 1, "PrecioUnitarioItem": 250.00, "MontoItem": 250.00 }] },
    "InformacionReferencia": { "NCFModificado": "E310000000001", "FechaNCFModificado": "14-05-2026", "CodigoModificacion": "3" }
  }
}
```

### Tipo 41 compras

```json
{
  "externalReference": "COMP-41-0001",
  "ECF": {
    "Encabezado": {
      "Version": "1.0",
      "IdDoc": { "TipoeCF": "41", "eNCF": "E410000000001", "TipoPago": "1" },
      "Emisor": { "RNCEmisor": "131880681", "RazonSocialEmisor": "ZYNSTORM SRL", "DireccionEmisor": "Av. Principal 1", "FechaEmision": "15-05-2026" },
      "Comprador": { "RNCComprador": "00112345678", "RazonSocialComprador": "PROVEEDOR INFORMAL" },
      "Totales": { "MontoExento": 800.00, "MontoTotal": 800.00 }
    },
    "DetallesItems": { "Item": [{ "NumeroLinea": "1", "IndicadorFacturacion": "4", "NombreItem": "Compra informal", "CantidadItem": 1, "PrecioUnitarioItem": 800.00, "MontoItem": 800.00 }] }
  }
}
```

### Tipo 43 gastos menores

```json
{
  "externalReference": "GM-43-0001",
  "ECF": {
    "Encabezado": {
      "Version": "1.0",
      "IdDoc": { "TipoeCF": "43", "eNCF": "E430000000001", "TipoPago": "1" },
      "Emisor": { "RNCEmisor": "131880681", "RazonSocialEmisor": "ZYNSTORM SRL", "DireccionEmisor": "Av. Principal 1", "FechaEmision": "15-05-2026" },
      "Comprador": {},
      "Totales": { "MontoExento": 300.00, "MontoTotal": 300.00 }
    },
    "DetallesItems": { "Item": [{ "NumeroLinea": "1", "IndicadorFacturacion": "4", "NombreItem": "Gasto menor", "CantidadItem": 1, "PrecioUnitarioItem": 300.00, "MontoItem": 300.00 }] }
  }
}
```

### Tipo 44 regimenes especiales

```json
{
  "externalReference": "REG-44-0001",
  "ECF": {
    "Encabezado": {
      "Version": "1.0",
      "IdDoc": { "TipoeCF": "44", "eNCF": "E440000000001", "TipoIngresos": "01", "TipoPago": "1" },
      "Emisor": { "RNCEmisor": "131880681", "RazonSocialEmisor": "ZYNSTORM SRL", "DireccionEmisor": "Av. Principal 1", "FechaEmision": "15-05-2026" },
      "Comprador": { "RNCComprador": "430000001", "RazonSocialComprador": "ENTIDAD REGIMEN ESPECIAL" },
      "Totales": { "MontoExento": 1200.00, "MontoTotal": 1200.00 }
    },
    "DetallesItems": { "Item": [{ "NumeroLinea": "1", "IndicadorFacturacion": "4", "NombreItem": "Servicio exento", "CantidadItem": 1, "PrecioUnitarioItem": 1200.00, "MontoItem": 1200.00 }] }
  }
}
```

### Tipo 45 gubernamental

```json
{
  "externalReference": "GOB-45-0001",
  "ECF": {
    "Encabezado": {
      "Version": "1.0",
      "IdDoc": { "TipoeCF": "45", "eNCF": "E450000000001", "TipoIngresos": "01", "TipoPago": "2", "FechaLimitePago": "30-05-2026" },
      "Emisor": { "RNCEmisor": "131880681", "RazonSocialEmisor": "ZYNSTORM SRL", "DireccionEmisor": "Av. Principal 1", "FechaEmision": "15-05-2026" },
      "Comprador": { "RNCComprador": "401000001", "RazonSocialComprador": "INSTITUCION PUBLICA" },
      "Totales": { "MontoExento": 2000.00, "MontoTotal": 2000.00 }
    },
    "DetallesItems": { "Item": [{ "NumeroLinea": "1", "IndicadorFacturacion": "4", "NombreItem": "Servicio gubernamental", "CantidadItem": 1, "PrecioUnitarioItem": 2000.00, "MontoItem": 2000.00 }] }
  }
}
```

### Tipo 46 exportacion

```json
{
  "externalReference": "EXP-46-0001",
  "ECF": {
    "Encabezado": {
      "Version": "1.0",
      "IdDoc": { "TipoeCF": "46", "eNCF": "E460000000001", "TipoIngresos": "01", "TipoPago": "1" },
      "Emisor": { "RNCEmisor": "131880681", "RazonSocialEmisor": "ZYNSTORM SRL", "DireccionEmisor": "Av. Principal 1", "FechaEmision": "15-05-2026" },
      "Comprador": { "IdentificadorExtranjero": "US-TAX-123456", "RazonSocialComprador": "FOREIGN CUSTOMER LLC", "PaisComprador": "US" },
      "Totales": { "MontoExento": 5000.00, "MontoTotal": 5000.00 }
    },
    "DetallesItems": { "Item": [{ "NumeroLinea": "1", "IndicadorFacturacion": "4", "NombreItem": "Servicio exportado", "CantidadItem": 1, "PrecioUnitarioItem": 5000.00, "MontoItem": 5000.00 }] }
  }
}
```

### Tipo 47 pagos al exterior

```json
{
  "externalReference": "EXT-47-0001",
  "ECF": {
    "Encabezado": {
      "Version": "1.0",
      "IdDoc": { "TipoeCF": "47", "eNCF": "E470000000001", "TipoPago": "1" },
      "Emisor": { "RNCEmisor": "131880681", "RazonSocialEmisor": "ZYNSTORM SRL", "DireccionEmisor": "Av. Principal 1", "FechaEmision": "15-05-2026" },
      "Comprador": { "IdentificadorExtranjero": "US-TAX-999999", "RazonSocialComprador": "SUPPLIER INC" },
      "Totales": { "MontoExento": 3500.00, "MontoTotal": 3500.00 }
    },
    "DetallesItems": { "Item": [{ "NumeroLinea": "1", "IndicadorFacturacion": "4", "NombreItem": "Pago servicio exterior", "CantidadItem": 1, "PrecioUnitarioItem": 3500.00, "MontoItem": 3500.00 }] }
  }
}
```
