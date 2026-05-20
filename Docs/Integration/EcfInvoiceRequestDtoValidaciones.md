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
| `ECF.Encabezado.IdDoc.TablaFormasPago.FormaDePago` | Obligatorio cuando se envia `TipoPago`. Cada forma debe incluir `FormaPago` y `MontoPago`. Para un solo pago tambien puede usar el atajo `FormaPago` y `MontoPago` dentro de `IdDoc`. |
| `ECF.Encabezado.Emisor.RNCEmisor` | RNC del emisor, solo digitos. |
| `ECF.Encabezado.Emisor.RazonSocialEmisor` | Nombre legal del emisor. |
| `ECF.Encabezado.Emisor.DireccionEmisor` | Direccion fiscal del emisor. |
| `ECF.Encabezado.Emisor.Telefono` | Telefono del emisor. Si `Comprador.TelefonoAdicional` no viene informado, se usa este mismo valor para generar `<TelefonoAdicional>` cuando la estructura de referencia lo incluye. |
| `ECF.Encabezado.Emisor.FechaEmision` | Fecha del comprobante. Formato recomendado: `dd-MM-yyyy`. |
| `ECF.Encabezado.Totales.MontoTotal` | Total del comprobante. |
| `ECF.DetallesItems.Item` | Debe contener al menos un item. |

Cada item debe traer:

| Campo | Regla |
| --- | --- |
| `NumeroLinea` | Numero de linea como texto o numero serializado. Ejemplo: `1`. |
| `IndicadorFacturacion` | Indicador DGII. Ejemplo: `1` para gravado ITBIS 18%, `4` para exento. |
| `NombreItem` | Nombre del producto o servicio. |
| `IndicadorBienoServicio` | Use `1` para bien y `2` para servicio. |
| `DescripcionItem` | Descripcion del producto o servicio. Se genera en el XML como `<DescripcionItem>`. |
| `CantidadItem` | Mayor que cero. |
| `UnidadMedida` | Obligatorio. Use `43` como unidad basica cuando no aplique una unidad mas especifica. |
| `PrecioUnitarioItem` | Cero o mayor. |
| `MontoItem` | Mayor que cero. Debe representar el monto neto de la linea luego de aplicar descuentos o recargos. |
| `AdditionalTaxRate` | Obligatorio solo si se envia `IscType`. |

## Unidades de medida basicas

El campo `UnidadMedida` debe enviarse con uno de los codigos DGII siguientes. Cuando no aplique una unidad mas especifica, use `43` (**Unidad**).

| Codigo | Descripcion | Codigo | Descripcion |
| --- | --- | --- | --- |
| `1` | Barril | `32` | Par |
| `2` | Bolsa | `33` | Pie |
| `3` | Bote | `34` | Pieza |
| `4` | Bultos | `35` | Rollo |
| `5` | Botella | `36` | Sobre |
| `6` | Caja/Cajon | `37` | Segundo |
| `7` | Cajetilla | `38` | Tanque |
| `8` | Centimetro | `39` | Tonelada |
| `9` | Cilindro | `40` | Tubo |
| `10` | Conjunto | `41` | Yarda |
| `11` | Contenedor | `42` | Yarda cuadrada |
| `12` | Dia | `43` | Unidad |
| `13` | Docena | `44` | Elemento |
| `14` | Fardo | `45` | Millar |
| `15` | Galones | `46` | Saco |
| `16` | Grado | `47` | Lata |
| `17` | Gramo | `48` | Display |
| `18` | Granel | `49` | Bidon |
| `19` | Hora | `50` | Racion |
| `20` | Huacal | `51` | Quintal |
| `21` | Kilogramo | `52` | Toneladas registro bruto |
| `22` | Kilovatio hora | `53` | Pie cuadrado |
| `23` | Libra | `54` | Pasajero |
| `24` | Litro | `55` | Pulgadas |
| `25` | Lote | `56` | Parqueo barcos muelle |
| `26` | Metro | `57` | Bandeja |
| `27` | Metro cuadrado | `58` | Hectarea |
| `28` | Metro cubico | `59` | Mililitro |
| `29` | Millones unidades termicas | `60` | Miligramo |
| `30` | Minuto | `61` | Onzas |
| `31` | Paquete | `62` | Onzas troy |

## Catalogos de EcfEnums

Los siguientes catalogos salen de `EcfEnums.cs`. No todos son obligatorios en todos los comprobantes; use el codigo cuando el campo correspondiente aplique.

### TipoPago - `ECF.Encabezado.IdDoc.TipoPago`

| Codigo | Descripcion | Ejemplo |
| --- | --- | --- |
| `1` | Pago al contado | `"TipoPago": "1"` |
| `2` | Venta a credito | `"TipoPago": "2", "FechaLimitePago": "30-05-2026"` |
| `3` | Entrega gratuita | `"TipoPago": "3"` |

### FormaPago - `ECF.Encabezado.IdDoc.TablaFormasPago.FormaDePago[].FormaPago`

El XML se genera como `<TablaFormasPago><FormaDePago><FormaPago>` y `<MontoPago>`. El `MontoPago` debe ser el monto cubierto por esa forma de pago; si solo hay una forma, normalmente coincide con `MontoTotal`.

| Codigo | Descripcion | Ejemplo |
| --- | --- | --- |
| `1` | Efectivo | `"FormaPago": "1"` |
| `2` | Cheque/Transferencia/Deposito | `"FormaPago": "2"` |
| `3` | Tarjeta de Debito/Credito | `"FormaPago": "3"` |
| `4` | Venta a Credito | `"FormaPago": "4"` |
| `5` | Bonos o certificados de regalo | `"FormaPago": "5"` |
| `6` | Permuta | `"FormaPago": "6"` |
| `7` | Nota de credito | `"FormaPago": "7"` |
| `8` | Otras formas de pago | `"FormaPago": "8"` |

### TipoIngresos - `ECF.Encabezado.IdDoc.TipoIngresos`

| Codigo | Descripcion | Ejemplo |
| --- | --- | --- |
| `1` | Ingresos por operaciones no financieros | `"TipoIngresos": "01"` |
| `2` | Ingresos financieros | `"TipoIngresos": "02"` |
| `3` | Ingresos extraordinarios | `"TipoIngresos": "03"` |
| `4` | Ingresos por arrendamientos | `"TipoIngresos": "04"` |
| `5` | Ingresos por venta de activo depreciable | `"TipoIngresos": "05"` |
| `6` | Otros ingresos | `"TipoIngresos": "06"` |

### IndicadorFacturacion - `Item[].IndicadorFacturacion`

| Codigo | Descripcion | Ejemplo |
| --- | --- | --- |
| `0` | No facturable (18%) | `"IndicadorFacturacion": "0"` |
| `1` | ITBIS 1 (18%) | `"IndicadorFacturacion": "1"` |
| `2` | ITBIS 2 (16%) | `"IndicadorFacturacion": "2"` |
| `3` | ITBIS 3 (0%) | `"IndicadorFacturacion": "3"` |
| `4` | Exento | `"IndicadorFacturacion": "4"` |

### IndicadorBienoServicio - `Item[].IndicadorBienoServicio`

| Codigo | Descripcion | Ejemplo |
| --- | --- | --- |
| `1` | Producto fisico o mercancia | `"IndicadorBienoServicio": "1"` |
| `2` | Prestacion de servicios | `"IndicadorBienoServicio": "2"` |

### CodigoModificacion - `ECF.InformacionReferencia.CodigoModificacion`

| Codigo | Descripcion | Ejemplo |
| --- | --- | --- |
| `1` | Anula el NCF modificado | `"CodigoModificacion": "1"` |
| `2` | Corrige texto del comprobante fiscal modificado | `"CodigoModificacion": "2"` |
| `3` | Corrige montos del NCF modificado | `"CodigoModificacion": "3"` |
| `4` | Reemplazo NCF emitido en contingencia | `"CodigoModificacion": "4"` |
| `5` | Referencia factura consumo electronica | `"CodigoModificacion": "5"` |

### ActividadEconomica - `ECF.Encabezado.Emisor.ActividadEconomica`

| Codigo | Descripcion | Ejemplo |
| --- | --- | --- |
| `471100` | Venta al por menor en supermercados | `"ActividadEconomica": "471100"` |
| `492310` | Transporte de carga | `"ActividadEconomica": "492310"` |
| `620200` | Consultoria informatica | `"ActividadEconomica": "620200"` |
| `691000` | Servicios juridicos | `"ActividadEconomica": "691000"` |
| `692020` | Servicios de contabilidad | `"ActividadEconomica": "692020"` |
| `749000` | Otras actividades profesionales | `"ActividadEconomica": "749000"` |

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
  "DescripcionItem": "Producto exento",
  "CantidadItem": 2,
  "UnidadMedida": "43",
  "PrecioUnitarioItem": 500,
  "MontoItem": 1000
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
  "DescripcionItem": "Servicio con descuento",
  "CantidadItem": 1,
  "UnidadMedida": "43",
  "PrecioUnitarioItem": 1000,
  "DescuentoMonto": 100,
  "MontoItem": 900
}
```

Si necesita detallar el descuento, use `TablaSubDescuento.SubDescuento`. La suma de los subdescuentos debe coincidir con `DescuentoMonto`.

```json
{
  "NumeroLinea": "1",
  "IndicadorFacturacion": "1",
  "NombreItem": "Servicio con subdescuento",
  "IndicadorBienoServicio": "2",
  "DescripcionItem": "Servicio con subdescuento",
  "CantidadItem": 1,
  "UnidadMedida": "43",
  "PrecioUnitarioItem": 1000,
  "DescuentoMonto": 100,
  "TablaSubDescuento": {
    "SubDescuento": [
      {
        "TipoSubDescuento": "$",
        "MontoSubDescuento": 100
      }
    ]
  },
  "MontoItem": 900
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
| `32` | Factura de consumo | Si `MontoTotal >= 250000`, enviar `Comprador.RNCComprador` o `Comprador.IdentificadorExtranjero`. Si es menor, el comprador puede ir vacio y DGII exige el canal de Resumen B2C (`RFCE`). |
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
        "TipoPago": "1",
        "TablaFormasPago": {
          "FormaDePago": [
            {
              "FormaPago": 1,
              "MontoPago": "1180.00"
            }
          ]
        }
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
        "MontoGravadoTotal": 1000,
        "ITBIS1": 18,
        "TotalITBIS": 180,
        "MontoTotal": 1180
      }
    },
    "DetallesItems": {
      "Item": [
        {
          "NumeroLinea": "1",
          "IndicadorFacturacion": "1",
          "NombreItem": "Servicio profesional",
          "IndicadorBienoServicio": "2",
          "DescripcionItem": "Servicio profesional",
          "CantidadItem": 1,
          "UnidadMedida": "43",
          "PrecioUnitarioItem": 1000,
          "MontoItem": 1000
        }
      ]
    }
  }
}
```

## Ejemplos por tipo

### Tipo 32 menor a RD$250,000

Para Facturas de Consumo (`TipoeCF: 32`) con `MontoTotal` menor a RD$250,000, la DGII requiere el envio por el canal de Resumen B2C (`RFCE`), no como e-CF individual. El API enruta estos casos automaticamente; no debe interpretarse como una nota de credito ni como otro tipo de documento. Para `MontoTotal` igual o mayor a RD$250,000, se envia como e-CF individual y puede requerir datos del comprador segun aplique.

```json
{
  "externalReference": "INV-32-0001",
  "ECF": {
    "Encabezado": {
      "Version": "1.0",
      "IdDoc": {
        "TipoeCF": "32",
        "eNCF": "E320000000001",
        "TipoIngresos": "01",
        "TipoPago": "1",
        "TablaFormasPago": {
          "FormaDePago": [
            {
              "FormaPago": 1,
              "MontoPago": "1500.00"
            }
          ]
        }
      },
      "Emisor": {
        "RNCEmisor": "131880681",
        "RazonSocialEmisor": "ZYNSTORM SRL",
        "DireccionEmisor": "Av. Principal 1",
        "FechaEmision": "15-05-2026"
      },
      "Comprador": {},
      "Totales": {
        "MontoExento": 1500,
        "MontoTotal": 1500
      }
    },
    "DetallesItems": {
      "Item": [
        {
          "NumeroLinea": "1",
          "IndicadorFacturacion": "4",
          "NombreItem": "Producto exento",
          "DescripcionItem": "Producto exento",
          "CantidadItem": 1,
          "UnidadMedida": "43",
          "PrecioUnitarioItem": 1500,
          "MontoItem": 1500
        }
      ]
    }
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
      "IdDoc": {
        "TipoeCF": "32",
        "eNCF": "E320000000003",
        "TipoIngresos": "01",
        "TipoPago": "1",
        "TablaFormasPago": {
          "FormaDePago": [
            {
              "FormaPago": 1,
              "MontoPago": "900.00"
            }
          ]
        }
      },
      "Emisor": {
        "RNCEmisor": "131880681",
        "RazonSocialEmisor": "ZYNSTORM SRL",
        "DireccionEmisor": "Av. Principal 1",
        "FechaEmision": "15-05-2026"
      },
      "Comprador": {},
      "Totales": {
        "MontoExento": 900,
        "MontoTotal": 900
      }
    },
    "DetallesItems": {
      "Item": [
        {
          "NumeroLinea": "1",
          "IndicadorFacturacion": "4",
          "NombreItem": "Producto exento con descuento",
          "IndicadorBienoServicio": "1",
          "DescripcionItem": "Producto exento con descuento",
          "CantidadItem": 1,
          "UnidadMedida": "43",
          "PrecioUnitarioItem": 1000,
          "DescuentoMonto": 100,
          "TablaSubDescuento": {
            "SubDescuento": [
              {
                "TipoSubDescuento": "$",
                "MontoSubDescuento": 100
              }
            ]
          },
          "MontoItem": 900
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
        "TerminoPago": "30 DIAS",
        "TablaFormasPago": {
          "FormaDePago": [
            {
              "FormaPago": 2,
              "MontoPago": "2627.80"
            }
          ]
        }
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
        "MontoTotal": 2627.8
      }
    },
    "DetallesItems": {
      "Item": [
        {
          "NumeroLinea": "1",
          "IndicadorFacturacion": "1",
          "NombreItem": "Cambio de Aceite (Labor)",
          "IndicadorBienoServicio": "1",
          "DescripcionItem": "Cambio de Aceite (Labor)",
          "CantidadItem": 1,
          "UnidadMedida": "43",
          "PrecioUnitarioItem": 1502,
          "MontoItem": 1502
        },
        {
          "NumeroLinea": "2",
          "IndicadorFacturacion": "1",
          "NombreItem": "Revision de Frenos",
          "IndicadorBienoServicio": "1",
          "DescripcionItem": "Revision de Frenos",
          "CantidadItem": 1,
          "UnidadMedida": "43",
          "PrecioUnitarioItem": 800,
          "MontoItem": 800
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
      "IdDoc": {
        "TipoeCF": "32",
        "eNCF": "E320000000002",
        "TipoIngresos": "01",
        "TipoPago": "1",
        "TablaFormasPago": {
          "FormaDePago": [
            {
              "FormaPago": 1,
              "MontoPago": "250000.00"
            }
          ]
        }
      },
      "Emisor": {
        "RNCEmisor": "131880681",
        "RazonSocialEmisor": "ZYNSTORM SRL",
        "DireccionEmisor": "Av. Principal 1",
        "FechaEmision": "15-05-2026"
      },
      "Comprador": {
        "RNCComprador": "00112345678",
        "RazonSocialComprador": "JUAN PEREZ"
      },
      "Totales": {
        "MontoExento": 250000,
        "MontoTotal": 250000
      }
    },
    "DetallesItems": {
      "Item": [
        {
          "NumeroLinea": "1",
          "IndicadorFacturacion": "4",
          "NombreItem": "Equipo",
          "DescripcionItem": "Equipo",
          "CantidadItem": 1,
          "UnidadMedida": "43",
          "PrecioUnitarioItem": 250000,
          "MontoItem": 250000
        }
      ]
    }
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
      "IdDoc": {
        "TipoeCF": "33",
        "eNCF": "E330000000001",
        "TipoIngresos": "01",
        "TipoPago": "1",
        "TablaFormasPago": {
          "FormaDePago": [
            {
              "FormaPago": 1,
              "MontoPago": "500.00"
            }
          ]
        }
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
        "MontoExento": 500,
        "MontoTotal": 500
      }
    },
    "DetallesItems": {
      "Item": [
        {
          "NumeroLinea": "1",
          "IndicadorFacturacion": "4",
          "NombreItem": "Ajuste por diferencia",
          "DescripcionItem": "Ajuste por diferencia",
          "CantidadItem": 1,
          "UnidadMedida": "43",
          "PrecioUnitarioItem": 500,
          "MontoItem": 500
        }
      ]
    },
    "InformacionReferencia": {
      "NCFModificado": "E310000000001",
      "FechaNCFModificado": "14-05-2026",
      "CodigoModificacion": "3"
    }
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
      "IdDoc": {
        "TipoeCF": "34",
        "eNCF": "E340000000001",
        "TipoIngresos": "01",
        "TipoPago": "1",
        "TablaFormasPago": {
          "FormaDePago": [
            {
              "FormaPago": 1,
              "MontoPago": "250.00"
            }
          ]
        }
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
        "MontoExento": 250,
        "MontoTotal": 250
      }
    },
    "DetallesItems": {
      "Item": [
        {
          "NumeroLinea": "1",
          "IndicadorFacturacion": "4",
          "NombreItem": "Devolucion parcial",
          "DescripcionItem": "Devolucion parcial",
          "CantidadItem": 1,
          "UnidadMedida": "43",
          "PrecioUnitarioItem": 250,
          "MontoItem": 250
        }
      ]
    },
    "InformacionReferencia": {
      "NCFModificado": "E310000000001",
      "FechaNCFModificado": "14-05-2026",
      "CodigoModificacion": "3"
    }
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
      "IdDoc": {
        "TipoeCF": "41",
        "eNCF": "E410000000001",
        "TipoPago": "1",
        "TablaFormasPago": {
          "FormaDePago": [
            {
              "FormaPago": 1,
              "MontoPago": "800.00"
            }
          ]
        }
      },
      "Emisor": {
        "RNCEmisor": "131880681",
        "RazonSocialEmisor": "ZYNSTORM SRL",
        "DireccionEmisor": "Av. Principal 1",
        "FechaEmision": "15-05-2026"
      },
      "Comprador": {
        "RNCComprador": "00112345678",
        "RazonSocialComprador": "PROVEEDOR INFORMAL"
      },
      "Totales": {
        "MontoExento": 800,
        "MontoTotal": 800
      }
    },
    "DetallesItems": {
      "Item": [
        {
          "NumeroLinea": "1",
          "IndicadorFacturacion": "4",
          "NombreItem": "Compra informal",
          "DescripcionItem": "Compra informal",
          "CantidadItem": 1,
          "UnidadMedida": "43",
          "PrecioUnitarioItem": 800,
          "MontoItem": 800
        }
      ]
    }
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
      "IdDoc": {
        "TipoeCF": "43",
        "eNCF": "E430000000001",
        "TipoPago": "1",
        "TablaFormasPago": {
          "FormaDePago": [
            {
              "FormaPago": 1,
              "MontoPago": "300.00"
            }
          ]
        }
      },
      "Emisor": {
        "RNCEmisor": "131880681",
        "RazonSocialEmisor": "ZYNSTORM SRL",
        "DireccionEmisor": "Av. Principal 1",
        "FechaEmision": "15-05-2026"
      },
      "Comprador": {},
      "Totales": {
        "MontoExento": 300,
        "MontoTotal": 300
      }
    },
    "DetallesItems": {
      "Item": [
        {
          "NumeroLinea": "1",
          "IndicadorFacturacion": "4",
          "NombreItem": "Gasto menor",
          "DescripcionItem": "Gasto menor",
          "CantidadItem": 1,
          "UnidadMedida": "43",
          "PrecioUnitarioItem": 300,
          "MontoItem": 300
        }
      ]
    }
  }
}
```

### Tipo 44 regimenes especiales

Para Regimenes Especiales (`TipoeCF: 44`), la DGII no acepta montos gravados ni totales de ITBIS dentro de `ECF.Encabezado.Totales`. Los items deben reportarse como exentos usando `IndicadorFacturacion: "4"` y los totales deben usar `MontoExento` y `MontoTotal`. No envie `MontoGravadoTotal`, `MontoGravadoI1`, `MontoGravadoI2`, `MontoGravadoI3`, `ITBIS1`, `ITBIS2`, `ITBIS3`, `TotalITBIS`, `TotalITBIS1`, `TotalITBIS2` ni `TotalITBIS3` para este tipo. Si aplica impuesto adicional, use `MontoImpuestoAdicional` / `ImpuestosAdicionales` segun corresponda.

```json
{
  "externalReference": "REG-44-0001",
  "ECF": {
    "Encabezado": {
      "Version": "1.0",
      "IdDoc": {
        "TipoeCF": "44",
        "eNCF": "E440000000001",
        "TipoIngresos": "01",
        "TipoPago": "1",
        "TablaFormasPago": {
          "FormaDePago": [
            {
              "FormaPago": 1,
              "MontoPago": "1200.00"
            }
          ]
        }
      },
      "Emisor": {
        "RNCEmisor": "131880681",
        "RazonSocialEmisor": "ZYNSTORM SRL",
        "DireccionEmisor": "Av. Principal 1",
        "FechaEmision": "15-05-2026"
      },
      "Comprador": {
        "RNCComprador": "430000001",
        "RazonSocialComprador": "ENTIDAD REGIMEN ESPECIAL"
      },
      "Totales": {
        "MontoExento": 1200,
        "MontoTotal": 1200
      }
    },
    "DetallesItems": {
      "Item": [
        {
          "NumeroLinea": "1",
          "IndicadorFacturacion": "4",
          "NombreItem": "Servicio exento",
          "DescripcionItem": "Servicio exento",
          "CantidadItem": 1,
          "UnidadMedida": "43",
          "PrecioUnitarioItem": 1200,
          "MontoItem": 1200
        }
      ]
    }
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
      "IdDoc": {
        "TipoeCF": "45",
        "eNCF": "E450000000001",
        "TipoIngresos": "01",
        "TipoPago": "2",
        "FechaLimitePago": "30-05-2026",
        "TablaFormasPago": {
          "FormaDePago": [
            {
              "FormaPago": 2,
              "MontoPago": "2000.00"
            }
          ]
        }
      },
      "Emisor": {
        "RNCEmisor": "131880681",
        "RazonSocialEmisor": "ZYNSTORM SRL",
        "DireccionEmisor": "Av. Principal 1",
        "FechaEmision": "15-05-2026"
      },
      "Comprador": {
        "RNCComprador": "401000001",
        "RazonSocialComprador": "INSTITUCION PUBLICA"
      },
      "Totales": {
        "MontoExento": 2000,
        "MontoTotal": 2000
      }
    },
    "DetallesItems": {
      "Item": [
        {
          "NumeroLinea": "1",
          "IndicadorFacturacion": "4",
          "NombreItem": "Servicio gubernamental",
          "DescripcionItem": "Servicio gubernamental",
          "CantidadItem": 1,
          "UnidadMedida": "43",
          "PrecioUnitarioItem": 2000,
          "MontoItem": 2000
        }
      ]
    }
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
      "IdDoc": {
        "TipoeCF": "46",
        "eNCF": "E460000000001",
        "TipoIngresos": "01",
        "TipoPago": "1",
        "TablaFormasPago": {
          "FormaDePago": [
            {
              "FormaPago": 1,
              "MontoPago": "5000.00"
            }
          ]
        }
      },
      "Emisor": {
        "RNCEmisor": "131880681",
        "RazonSocialEmisor": "ZYNSTORM SRL",
        "DireccionEmisor": "Av. Principal 1",
        "FechaEmision": "15-05-2026"
      },
      "Comprador": {
        "IdentificadorExtranjero": "US-TAX-123456",
        "RazonSocialComprador": "FOREIGN CUSTOMER LLC",
        "PaisComprador": "US"
      },
      "Totales": {
        "MontoExento": 5000,
        "MontoTotal": 5000
      }
    },
    "DetallesItems": {
      "Item": [
        {
          "NumeroLinea": "1",
          "IndicadorFacturacion": "4",
          "NombreItem": "Servicio exportado",
          "DescripcionItem": "Servicio exportado",
          "CantidadItem": 1,
          "UnidadMedida": "43",
          "PrecioUnitarioItem": 5000,
          "MontoItem": 5000
        }
      ]
    }
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
      "IdDoc": {
        "TipoeCF": "47",
        "eNCF": "E470000000001",
        "TipoPago": "1",
        "TablaFormasPago": {
          "FormaDePago": [
            {
              "FormaPago": 1,
              "MontoPago": "3500.00"
            }
          ]
        }
      },
      "Emisor": {
        "RNCEmisor": "131880681",
        "RazonSocialEmisor": "ZYNSTORM SRL",
        "DireccionEmisor": "Av. Principal 1",
        "FechaEmision": "15-05-2026"
      },
      "Comprador": {
        "IdentificadorExtranjero": "US-TAX-999999",
        "RazonSocialComprador": "SUPPLIER INC"
      },
      "Totales": {
        "MontoExento": 3500,
        "MontoTotal": 3500
      }
    },
    "DetallesItems": {
      "Item": [
        {
          "NumeroLinea": "1",
          "IndicadorFacturacion": "4",
          "NombreItem": "Pago servicio exterior",
          "DescripcionItem": "Pago servicio exterior",
          "CantidadItem": 1,
          "UnidadMedida": "43",
          "PrecioUnitarioItem": 3500,
          "MontoItem": 3500
        }
      ]
    }
  }
}
```

## Consumo del EcfController

Los endpoints de emision reciben y responden JSON. Todos los requests deben incluir la autenticacion configurada para el API, usando el header de API Key que corresponda en su ambiente.

Base de rutas:

| Ambiente | Ruta base |
| --- | --- |
| Local | `https://localhost:{puerto}/v1/Ecf` |
| Staging/Produccion | `https://{host}/v1/Ecf` |

Headers recomendados:

| Header | Valor |
| --- | --- |
| `Content-Type` | `application/json` |
| `Accept` | `application/json` |
| `X-Api-Key` | API Key asignada al cliente. Use el nombre real del header si el ambiente fue configurado con otro nombre. |

### EmitEcf

Emite un e-CF a partir del `EcfInvoiceRequestDto`, genera el XML, valida contra XSD y referencias `XmlProd`, firma el XML, envia el comprobante y consulta el estado inicial.

```http
POST /v1/Ecf/emit?environment=Production
```

`environment` es opcional. Valores permitidos:

| Valor | Uso |
| --- | --- |
| `Production` | Envia a DGII produccion. |
| `Test` | Ambiente de prueba si esta configurado. |
| `CerteCF` | Ambiente de certificacion si esta configurado. |

En `Development` y `Staging`, el servicio puede validar el XML generado contra el endpoint de validacion configurado antes de enviarlo a DGII, segun la configuracion del ambiente.

Ejemplo con `curl`:

```bash
curl -X POST "https://localhost:5001/v1/Ecf/emit?environment=Production" \
  -H "Content-Type: application/json" \
  -H "Accept: application/json" \
  -H "X-Api-Key: {API_KEY}" \
  -d @factura.json
```

Ejemplo con `fetch`:

```javascript
const response = await fetch("https://localhost:5001/v1/Ecf/emit?environment=Production", {
  method: "POST",
  headers: {
    "Content-Type": "application/json",
    "Accept": "application/json",
    "X-Api-Key": apiKey
  },
  body: JSON.stringify(ecfInvoiceRequestDto)
});

const result = await response.json();
```

#### Respuesta cuando DGII acepta dentro de la ventana inicial

El API espera hasta 2 segundos por una respuesta final. La primera consulta de estado se hace luego de 300 milisegundos. Si DGII responde `Aceptado` dentro de esa ventana, `success` llega en `true`, `isPending` llega en `false` y se incluyen los datos principales de verificacion.

```json
{
  "success": true,
  "isPending": false,
  "message": "TrackId: 123456789",
  "ecfDocumentId": 1001,
  "ecfType": 31,
  "eNcf": "E310000000001",
  "trackId": "123456789",
  "securityCode": "ABC123",
  "signatureDate": "16-05-2026 10:35:20",
  "qrUrl": "https://ecf.dgii.gov.do/CerteCF/ConsultaTimbre?...",
  "status": {
    "trackId": "123456789",
    "estado": "Aceptado",
    "mensaje": "Aceptado"
  }
}
```

`qrUrl` contiene la URL que debe usarse para generar el QR con la libreria que el cliente prefiera. El API ya no devuelve la imagen QR ni el objeto `xmlValidation` en la respuesta de `EmitEcf`.

#### Respuesta cuando DGII sigue procesando

Si despues de 2 segundos DGII no confirma aceptacion o rechazo, el API devuelve el `TrackId`, marca `isPending` en `true` y programa el job de seguimiento. El cliente debe consultar el endpoint de estado.

```json
{
  "success": false,
  "isPending": true,
  "message": "DGII aun procesa el e-CF. TrackId: 123456789",
  "ecfDocumentId": 1001,
  "ecfType": 31,
  "eNcf": "E310000000001",
  "trackId": "123456789",
  "securityCode": "ABC123",
  "signatureDate": "16-05-2026 10:35:20",
  "qrUrl": "https://ecf.dgii.gov.do/CerteCF/ConsultaTimbre?...",
  "hangfireJobId": "456",
  "status": {
    "trackId": "123456789",
    "estado": "Recibido",
    "mensaje": "Recibido"
  }
}
```

#### Respuesta con errores de validacion

Si el objeto recibido no cumple las reglas del DTO, XSD, `XmlProd` o reglas internas, el API responde `400 Bad Request` con el mismo objeto de resultado y las listas de errores llenas.

```json
{
  "success": false,
  "message": "El XML generado no cumple con el esquema XSD de la DGII.",
  "dtoErrors": [],
  "xsdErrors": [
    "Detalle del error XSD"
  ],
  "xmlProdErrors": []
}
```

### GetEmissionStatus

Consulta el estado cacheado del envio por `TrackId`. Este estado se actualiza con la consulta inicial y con el job de seguimiento.

```http
GET /v1/Ecf/status/{trackId}
GET /v1/Ecf/estado-envio/{trackId}
```

Ejemplo con `curl`:

```bash
curl -X GET "https://localhost:5001/v1/Ecf/status/123456789" \
  -H "Accept: application/json" \
  -H "X-Api-Key: {API_KEY}"
```

Ejemplo con `fetch`:

```javascript
const response = await fetch(`https://localhost:5001/v1/Ecf/status/${trackId}`, {
  headers: {
    "Accept": "application/json",
    "X-Api-Key": apiKey
  }
});

const statusResult = await response.json();
```

Respuesta cuando el estado existe:

```json
{
  "success": true,
  "isPending": false,
  "trackId": "123456789",
  "status": {
    "trackId": "123456789",
    "codigo": "1",
    "estado": "Aceptado",
    "rnc": "131880681",
    "eNcf": "E310000000001",
    "secuenciaUtilizada": true,
    "fechaRecepcion": "16-05-2026 10:35:22",
    "error": "",
    "mensaje": "Aceptado",
    "mensajes": []
  }
}
```

Cuando `isPending` llega en `true`, el cliente debe esperar unos segundos y consultar nuevamente con el mismo `TrackId`. Cuando `success` llega en `false` y `isPending` llega en `false`, el comprobante fue rechazado o quedo en error; revise `status.error`, `status.mensaje` y `status.mensajes`.

Respuesta cuando el `TrackId` no esta en cache o expiro:

```json
{
  "success": false,
  "isPending": true,
  "trackId": "123456789",
  "message": "Estado no encontrado o expirado. Si el envio fue reciente, intente consultar nuevamente en unos segundos."
}
```
