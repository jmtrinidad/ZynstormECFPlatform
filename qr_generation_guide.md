# Guía Técnica para la Generación de Códigos QR (e-CF DGII)

Esta guía detalla las especificaciones técnicas requeridas para construir la URL del timbre fiscal integrada en los códigos QR de las representaciones impresas (RI).

---

## 1. Facturas Normales y Comprobantes Generales
Aplica para comprobantes del tipo **E31, E33, E34, E41, E43, E44, E45, E46, E47** y facturas de consumo (**E32**) con montos iguales o mayores a RD$ 250,000.00.

### URL Base (Entorno de Certificación / Pruebas)
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
| `CodigoSeguridad` | Hash de validación | Primeros 6 caracteres del `<SignatureValue>` del XML |

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
