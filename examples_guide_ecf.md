# JSON Payload Examples Guide

This guide provides independent usage examples for every document type. Use these as templates for your frontend integration.

````carousel
```json
// TYPE 31: Factura de Crédito Electrónica
// Use: Standard B2B transactions between taxpayers.
{
  "ncf": "E310000000001",
  "externalReference": "INV-001",
  "issueDate": "2026-04-21T00:00:00",
  "issuerRnc": "131880681",
  "issuerName": "MI EMPRESA SRL",
  "issuerAddress": "Av. Winston Churchill #123",
  "customerRnc": "101010101", // Mandatory valid RNC
  "customerName": "CLIENTE CORPORATIVO SAS",
  "paymentType": 1, // 1: Contado, 2: Credito
  "items": [
    {
      "name": "Servicio de Consultoría",
      "quantity": 1,
      "unitPrice": 1000.00,
      "taxPercentage": 18
    }
  ]
}
```
<!-- slide -->
```json
// TYPE 32 (Full): Factura de Consumo (>= 250k)
// Use: Personal consumption where the amount requires identification.
{
  "ncf": "E320000000001",
  "externalReference": "CON-001",
  "issueDate": "2026-04-21T00:00:00",
  "issuerRnc": "131880681",
  "issuerName": "MI EMPRESA SRL",
  "issuerAddress": "Av. Winston Churchill #123",
  "customerRnc": "00112345678", // Real Cédula/RNC required
  "customerName": "JUAN PEREZ",
  "items": [
    {
      "name": "Mueble de Oficina",
      "quantity": 1,
      "unitPrice": 250000.00,
      "taxPercentage": 18
    }
  ]
}
```
<!-- slide -->
```json
// TYPE 32 (Small/RFCE): Factura de Consumo (< 250k)
// Use: B2C sales to anonymous consumers. 
{
  "ncf": "E320000000002",
  "externalReference": "CON-002",
  "issueDate": "2026-04-21T00:00:00",
  "issuerRnc": "131880681",
  "issuerName": "MI EMPRESA SRL",
  "issuerAddress": "Av. Winston Churchill #123",
  "customerRnc": null, // Optional/Empty for small consumption
  "customerName": "Consumidor Final", // Standard name
  "items": [
    {
      "name": "Articulo Menor",
      "quantity": 2,
      "unitPrice": 500.00,
      "taxPercentage": 18
    }
  ]
}
```
<!-- slide -->
```json
// TYPE 33: Nota de Débito Electrónica
// Use: To increase the balance of a previous invoice.
{
  "ncf": "E330000000001",
  "externalReference": "ND-001",
  "issueDate": "2026-04-21T00:00:00",
  "issuerRnc": "131880681",
  "issuerName": "MI EMPRESA SRL",
  "issuerAddress": "Av. Winston Churchill #123",
  "customerRnc": "101010101",
  "customerName": "CLIENTE CORPORATIVO SAS",
  "referenceNcf": "E310000000001", // Mandatory
  "referenceIssueDate": "2026-04-20T00:00:00", // Mandatory
  "referenceReasonCode": 3, // 3: Corrección de montos
  "items": [
    {
      "name": "Cargo Adicional por Flete",
      "quantity": 1,
      "unitPrice": 500.00,
      "taxPercentage": 18
    }
  ]
}
```
<!-- slide -->
```json
// TYPE 34: Nota de Crédito Electrónica
// Use: To decrease/anul an invoice or record a return.
{
  "ncf": "E340000000001",
  "externalReference": "NC-001",
  "issueDate": "2026-04-21T00:00:00",
  "issuerRnc": "131880681",
  "issuerName": "MI EMPRESA SRL",
  "issuerAddress": "Av. Winston Churchill #123",
  "customerRnc": "101010101",
  "customerName": "CLIENTE CORPORATIVO SAS",
  "referenceNcf": "E310000000001", // Mandatory
  "referenceIssueDate": "2026-04-20T00:00:00",
  "referenceReasonCode": 1, // 1: Anulación total
  "items": [
    {
      "name": "Devolución de Mercancía",
      "quantity": 1,
      "unitPrice": 1000.00,
      "taxPercentage": 18
    }
  ]
}
```
<!-- slide -->
```json
// TYPE 41: Registro de Proveedores Informales
// Use: Buying from someone not registered as a taxpayer.
{
  "ncf": "E410000000001",
  "externalReference": "PROV-001",
  "issueDate": "2026-04-21T00:00:00",
  "issuerRnc": "131880681",
  "issuerName": "MI EMPRESA SRL",
  "issuerAddress": "Av. Winston Churchill #123",
  "customerRnc": "00199999999", // Provider's Cédula
  "customerName": "PEDRO PROVEEDOR",
  "items": [
    {
      "name": "Reparación Local",
      "quantity": 1,
      "unitPrice": 5000.00,
      "taxPercentage": 18,
      "isrRetentionAmount": 500.00 // 10% ISR Retention
    }
  ]
}
```
<!-- slide -->
```json
// TYPE 44: Regímenes Especiales
// Use: Selling to entities with tax exemptions (Free Zones, Tourism, etc).
{
  "ncf": "E440000000001",
  "externalReference": "ZONA-001",
  "issueDate": "2026-04-21T00:00:00",
  "issuerRnc": "131880681",
  "issuerName": "MI EMPRESA SRL",
  "issuerAddress": "Av. Winston Churchill #123",
  "customerRnc": "101222222", // Exempt Client RNC
  "customerName": "ZONA FRANCA EXPORT",
  "items": [
    {
      "name": "Venta Exenta",
      "quantity": 10,
      "unitPrice": 500.00,
      "taxPercentage": 0, // Mandatory 0% for Special Regimes
      "billingIndicator": 4 // 4: Exento
    }
  ]
}
```
<!-- slide -->
```json
// TYPE 46: Pagos al Exterior
// Use: Paying services to non-residents.
{
  "ncf": "E460000000001",
  "externalReference": "EXT-001",
  "issueDate": "2026-04-21T00:00:00",
  "issuerRnc": "131880681",
  "issuerName": "MI EMPRESA SRL",
  "customerForeignId": "US-TAX-123456", // Passport/Tax ID
  "customerCountry": "Estados Unidos",
  "customerName": "GOOGLE CLOUD SERVICES",
  "items": [
    {
      "name": "Suscripción Digital",
      "quantity": 1,
      "unitPrice": 100.00,
      "taxPercentage": 18
    }
  ]
}
```
<!-- slide -->
```json
// AEC: Aprobación Comercial
// Use: To accept or reject an e-CF received from a provider.
{
  "rncEmisor": "131880681", // Provider RNC
  "eNcf": "E310000000005",
  "fechaEmision": "01-04-2020",
  "montoTotal": 83320.00,
  "rncComprador": "133009889", // My RNC
  "estado": 1, // 1: Aprobado, 2: Rechazado
  "detalleMotivoRechazo": "",
  "fechaHoraAprobacionComercial": "21-04-2026 10:45:05"
}
```
````

---

### Key Reminders for Frontend:
- **Date Format**: `EcfInvoiceRequestDto` uses ISO 8601 (`"yyyy-MM-ddT00:00:00"`). `AcecfRequestDto` uses custom strings (`"dd-MM-yyyy"` and `"dd-MM-yyyy HH:mm:ss"`).
- **Decimals**: Always include at least 2 decimal places in monetary values for better precision in pre-calculations.
- **NCF Logic**: The first 3 digits of the NCF (e.g., `E31`, `E32`) must match the intended document type.
