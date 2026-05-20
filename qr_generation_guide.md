# Guía Técnica para la Generación de Códigos QR (e-CF DGII)

Esta guía detalla las especificaciones técnicas requeridas para construir la URL del timbre fiscal integrada en los códigos QR de las representaciones impresas (RI).

---

## 1. Facturas Normales y Comprobantes Generales
Aplica para comprobantes del tipo **E31, E33, E34, E41, E43, E44, E45, E46, E47** y facturas de consumo (**E32**) con montos iguales o mayores a RD$ 250,000.00.

### URL Base (Entorno de Pruebas/Test **y** Certificación)
> ⚠️ La DGII **no expone** el portal `ConsultaTimbre` bajo `/TesteCF/`. Aunque el XML se envía y acepta en `TesteCF`, el QR siempre debe apuntar a `/CerteCF/ConsultaTimbre` en ambientes no-productivos.

`https://ecf.dgii.gov.do/CerteCF/ConsultaTimbre?`

### Parámetros Requeridos (En Orden)
| Parámetro | Descripción | Formato / Regla |
|---|---|---|
| `RncEmisor` | RNC del emisor | Solo números (ej. `102620717`) |
| `RncComprador` | RNC del receptor | Solo números. *Omitir parámetro si está vacío.* |
| `ENCF` | NCF Electrónico | Ej. `E310000013206` |
| `FechaEmision` | Fecha de expedición | `DD-MM-YYYY` (ej. `30-03-2026`) |
| `MontoTotal` | Total facturado | Sin decimales superfluos (ej. `53100` o `6029.50`) |
| `FechaFirma` | Fecha de firmado | `DD-MM-YYYY%20HH:mm:ss` (escapar espacio, dos puntos literales) |
| `CodigoSeguridad` | Hash de validación | Primeros 6 caracteres **RAW** del `<SignatureValue>` del XML firmado. **⚠️ Case-sensitive**: la DGII almacena el valor tal como viene en el base64. Ej: si el `SignatureValue` empieza con `bUfoV4jM...` el código es `bUfoV4`, NO `BUFOV4`. Solo eliminar saltos de línea/espacios del base64 multilínea, nunca convertir a mayúsculas. |

---

## 2. Facturas de Consumo Menores de RD$ 250,000.00
Aplica exclusivamente para comprobantes de tipo **E32** cuyo monto sea estrictamente inferior a RD$ 250,000.00.

### URL Base (Entorno de Certificación / Pruebas)
`https://fc.dgii.gov.do/CerteCF/ConsultaTimbreFC?`

### Parámetros Requeridos (En Orden)
| Parámetro | Descripción | Formato / Regla |
|---|---|---|
| `RncEmisor` | RNC del emisor | Solo números |
| `ENCF` | NCF Electrónico | Ej. `E320000000344` |
| `MontoTotal` | Total facturado | Sin decimales superfluos |
| `CodigoSeguridad` | Hash de validación | Primeros 6 caracteres del `<SignatureValue>` o `<CodigoSeguridadeCF>` del XML |

---

## Consideraciones Adicionales
1. **Codificación URL:** El `CodigoSeguridad` debe ser escapado apropiadamente utilizando `Uri.EscapeDataString` para preservar caracteres especiales como `+` o `/`.
2. **Espacios en Fecha:** Para `FechaFirma`, los espacios deben ser reemplazados explícitamente por `%20`.
