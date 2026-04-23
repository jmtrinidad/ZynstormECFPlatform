# JSON Integration Guide for Frontend

This guide provides the expected JSON structures for each type of electronic document (e-CF) and Commercial Approval (AEC) supported by the platform.

## Base Schema: `EcfInvoiceRequestDto`

All e-CF types (31 to 47) use the same base object. The platform automatically determines the document behavior based on the `Ncf` prefix and the values provided.

### Mandatory Generic Fields
```json
{
  "ncf": "E310000000001", // 13 chars
  "externalReference": "INV-2026-001",
  "issueDate": "2026-04-21T00:00:00", // ISO 8601
  "issuerRnc": "131880681",
  "issuerName": "ZYNSTORM SRL",
  "issuerAddress": "Calle Principal #1, Santo Domingo",
  "customerRnc": "131880681", // Varies by type
  "customerName": "CLIENTE DE PRUEBA", // Varies by type
  "items": [
    {
      "name": "Producto de Prueba",
      "quantity": 1,
      "unitPrice": 100.00,
      "taxPercentage": 18,
      "itbisAmount": 18.00 // Optional, if 0 it is calculated
    }
  ]
}
```

---

## 1. Credit-Based Invoices (Type 31 / 45)
Standard invoices between taxpayers (B2B) or Govt.
- **`customerRnc`**: Mandatory (9 or 11 digits).
- **`customerName`**: Mandatory legal name.

## 2. Consumer Invoices (Type 32)
Behavior depends on the transaction amount.

### Case A: Amount >= RD$ 250,000.00
- **`customerRnc`**: Mandatory (Cédula or RNC).
- **`customerName`**: Mandatory full name.

### Case B: Amount < RD$ 250,000.00 (Standard RFCE / B2C)
> [!IMPORTANT]
> To comply with DGII anonymization rules for small consumption, buyer fields must be generic.
- **`customerRnc`**: Use `null`, empty string, or `"222222222"`.
- **`customerName`**: Use `"Consumidor Final"`.
- **Logic**: The platform will automatically route this transaction to the B2C (RFCE) channel in the background.

---

## 3. Notes (Type 33 - Debit / 34 - Credit)
Used to modify a previously issued NCF.
- **`referenceNcf`**: Mandatory (The NCF being modified).
- **`referenceIssueDate`**: Mandatory.
- **`referenceReasonCode`**: Mandatory (1: Anula, 2: Corrige texto, 3: Corrige montos).
- **`referenceReasonDescription`**: Optional text explanation.

---

## 4. Informal Providers (Type 41 / 43)
Used when purchasing from non-taxpayers.
- **`items.isrRetentionAmount`**: Mandatory for Type 41.
- **`manualTotalITBISRetenido`**: Often required for certification test cases.

---

## 5. Foreign Payments (Type 46) / Exports (Type 47)
- **`customerForeignId`**: Mandatory (Passport, Foreign Tax ID).
- **`customerCountry`**: Mandatory (ISO Country Name or Code).
- **`customerRnc`**: Should be empty or generic.

---

## 6. Commercial Approval (AEC)
Structure for `AcecfRequestDto`. Used for Step 4 Performace or manual approvals.

```json
{
  "rncEmisor": "131880681",
  "eNcf": "E310000000001",
  "fechaEmision": "21-04-2026", // Format: dd-MM-yyyy
  "montoTotal": 118.00,
  "rncComprador": "133009889",
  "estado": 1, // 1: Aprobado, 2: Rechazado
  "detalleMotivoRechazo": "", // Required if estado = 2
  "fechaHoraAprobacionComercial": "21-04-2026 10:45:00" // Format: dd-MM-yyyy HH:mm:ss
}
```

---

## Summary of Special Fields

| Feature | Field(s) | Description |
| :--- | :--- | :--- |
| **RFCE** | `-` | Selected automatically if type=32 and total < 250k. |
| **ISC Tax** | `iscType`, `additionalTaxRate` | Used for Beer, Tobacco, Alcohol etc. |
| **Govt** | `incomeType` | Defaults to `"01"`. |
| **Manual Overrides** | `manualMontoTotal`, etc | Used **only** during certification to force values. |
