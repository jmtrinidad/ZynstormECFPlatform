# e-CF Roadmap: Firma y Transmisión

Este documento consolida el análisis técnico realizado sobre la generación de e-CF, las correcciones de esquemas XSD y los requisitos del manual técnico de la DGII.

## 1. Estado Actual: Generación XML
- **Validación XSD:** ✅ 10/10 tipos (31-47) pasan la validación estructural.
- **DTO Universal:** El `EcfInvoiceRequestDto` es suficiente para todos los tipos.
- **Base de Datos:** Se generó la migración `AddEcfDocumentMissingFields` para incluir campos de exportación y notas de crédito.

## 2. Requisitos de Firma Digital (XML-DSig)
Según la "Descripción Técnica de Facturación Electrónica" (pág. 60):
- **Algoritmo:** SHA-256.
- **Canonicalización:** `preserveWhitespace = false`.
- **Estructura:** La firma se inserta al final del elemento raíz `<ECF>`.
- **Namespace:** El elemento raíz suele transformarse en `<eCF xmlns="http://dgii.gov.do/ecf" version="1.0">` durante el proceso de firma.

## 3. Flujo de Autenticación y Envío
Para interactuar con los servicios de la DGII:
1. **GET Semilla:** Obtener el XML de semilla desde el endpoint de autenticación.
2. **Firma Semilla:** Firmar el XML semilla con el certificado digital.
3. **POST Token:** Enviar la semilla firmada para obtener un **Bearer Token JWT** (validez de 1 hora).
4. **POST e-CF:** Enviar el XML del e-CF firmado usando el token en el header `Authorization`.

## 4. Próximos Pasos (Implementación)

### Fase A: Firma Digital
1. [ ] Implementar un `EcfSignatureService` usando el certificado `.p12`/`.pfx`.
2. [ ] Asegurar que la firma cumpla con `SHA-256` y `preserveWhitespace = false`.
3. [ ] Probar la firma con el XML de "Semilla".

### Fase B: Comunicación con DGII
1. [ ] Crear un `EcfDgiiClient` para manejar los endpoints (Producción y Test).
2. [ ] Implementar el flujo de autenticación automática (obtener y refrescar token).
3. [ ] Implementar el envío de e-CF y captura del `trackId`.

### Fase C: Enrutamiento y Lógica de Negocio
1. [ ] Lógica de envío dual: Tipos 31, 33, 34 y 44 deben enviarse tanto a la DGII como al receptor.
2. [ ] Manejo de Factura de Consumo (Tipo 32): Lógica de "Resumen" para montos < 250k.
3. [ ] Procesamiento de respuestas (Aceptado, Rechazado, Condicional).

---
> [!NOTE]
> Para más detalles, consultar los archivos de análisis:
> - [Análisis de Esquemas y BD](file:///C:/Users/JoseTrinidad/.gemini/antigravity/brain/0372ec6e-a90d-4498-93e3-55f105555aef/ecf_analysis.md)
> - [Análisis Técnico PDF DGII](file:///C:/Users/JoseTrinidad/.gemini/antigravity/brain/0372ec6e-a90d-4498-93e3-55f105555aef/pdf_analysis.md)
