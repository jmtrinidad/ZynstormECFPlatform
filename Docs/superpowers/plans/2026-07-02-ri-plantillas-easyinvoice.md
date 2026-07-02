# RI con plantillas de EasyInvoice — Plan

> REQUIRED SUB-SKILL: superpowers:subagent-driven-development. Steps `- [ ]`.

**Goal:** Generar la RI de los comprobantes del Paso 4 con plantillas QuestPDF portadas de EasyInvoice (InvoicePdf para la mayoría, PurchasePdf hoja completa para tipo 41), con el QR de la DGII, sin subida.

**Tech:** .NET 10, QuestPDF (existente), QRCoder (existente). Fuentes de port: `\\wsl.localhost\Ubuntu\home\dev\projects\EasyInvoice-WorkSpace\EasyInvoice\EasyInvoice.Reports\Invoices\InvoicePdf.cs` y `...\Purchases\PurchasePdf.cs`.

## Global Constraints
- QR: URL con `EcfQrUrlBuilder.Build(...)` (Core.Ecf), ambiente `CerteCF`. Datos: `EcfRiDataMapper`/nuevo mapper desde el `XmlSent`.
- Tipo→plantilla: 41→PurchasePdf; resto (31,32,33,34,43,44,45,46,47)→InvoicePdf.
- QuestPDF `LicenseType.Community` en static ctor.
- Tests en `ZynstormECFPlatform.Tests`. Build `dotnet build ZynstormECFPlatform.slnx -nologo`.
- Retirar: RiModelExtractor, RiPdfRenderer(PdfSharp) [reescrito], PageModel, RiFontResolver+Fonts, paquete PdfSharp, endpoints de subida, UI de subir.

---

## Task P1: View-models + mapper + RiInvoicePdf portado
**Files:** Create `ZynstormECFPlatform.Services/Ri/RiInvoiceModel.cs`, `EcfRiTemplateMapper.cs`, `RiInvoicePdf.cs`. Test `ZynstormECFPlatform.Tests/Ri/EcfRiTemplateMapperTests.cs`, `RiInvoicePdfTests.cs`.
**Interfaces:** `EcfRiTemplateMapper.MapInvoice(string signedXml, DgiiEnvironment env) : RiInvoiceModel`; `RiInvoicePdf(RiInvoiceModel model) : IDocument`.
- [ ] Portar de EasyInvoice `InvoicePdf.cs` a `RiInvoicePdf` adaptando a `RiInvoiceModel` (POCO con solo los campos usados: Company{Name,Rnc,Address,Phone,Municipality?}, Client{Name,Rnc,Address?}, Ncf/tipo+nombre, Number/fechaEmision/fechaFirma, Items[]{Description,Qty,Price,Itbis,Amount,Discount?}, SubTotal,Discount,Itbis,Total, Note?, PaymentType?, Qr string, SecurityCode). Copiar helpers mínimos (GetQueryParam, formato NCF, Culture) al proyecto. Mantener el bloque QR existente (QRCoder→Image) leyendo `model.Qr`.
- [ ] `EcfRiTemplateMapper.MapInvoice`: XML→RiInvoiceModel (reusar la lógica de `EcfRiDataMapper` para emisor/comprador/ítems/totales/securityCode) y `Qr = EcfQrUrlBuilder.Build(env, tipo, ...)`.
- [ ] Tests: mapper con XML reales (E31/E32<250k) → view-model correcto + Qr portal correcto. Humo RiInvoicePdf: `%PDF`, y PdfPig-read contiene NCF, total y Código de Seguridad. RED→GREEN. Build slnx verde. Commit.

## Task P2: RiPurchasePdf portado (hoja completa) + QR
**Files:** Create `ZynstormECFPlatform.Services/Ri/RiPurchaseModel.cs`, `RiPurchasePdf.cs`. Test `RiPurchasePdfTests.cs`. Extend `EcfRiTemplateMapper.MapPurchase`.
- [ ] Portar `PurchasePdf.cs` a `RiPurchasePdf` (PageSizes.Letter) adaptado a `RiPurchaseModel`; **agregar bloque QR** (QRCoder→`.Image()`) + Código de Seguridad en un lugar visible (footer/esquina) leyendo `model.Qr`.
- [ ] `MapPurchase(xml,env)` → RiPurchaseModel (emisor=Company, comprador=Supplier). Test humo: tipo 41 → `%PDF` no vacío, contiene NCF + Código de Seguridad. Build verde. Commit.

## Task P3: RiPdfRenderer (dispatch por tipo) + servicio + retiro
**Files:** Rewrite `RiPdfRenderer.cs`; modify `CertificationRiModelService.cs`; delete `RiModelExtractor.cs`, `PageModel.cs`, `RiFontResolver.cs`, `Ri/Fonts/*`; edit Services.csproj (quitar PdfSharp).
- [ ] `RiPdfRenderer.Render(int ecfType, string signedXml) : byte[]`: 41→`new RiPurchasePdf(MapPurchase(...)).GeneratePdf()`; resto→`new RiInvoicePdf(MapInvoice(...)).GeneratePdf()`.
- [ ] `CertificationRiModelService`: quitar Upload/Update/List/GetReferenceRi/Delete/extracción; dejar `RenderRiForDocumentAsync`/`RenderAllZipAsync` usando el nuevo renderer (por `EcfTypeId`/NCF). Quitar (de)serialización de PageModel/LayoutJson.
- [ ] Eliminar extractor/PageModel/RiFontResolver+Fonts; quitar `PdfSharp` del csproj. `dotnet build slnx` verde; `dotnet test` todo pasa. Commit.

## Task P4: Controller — quitar endpoints de subida
**Files:** Modify `CertificationController.cs`.
- [ ] Quitar `POST/PUT/GET print-templates*`, `DELETE print-templates/*` y la inyección relacionada si queda sin uso. Mantener `GET ri/{clientGuidId}/{ncf}/preview|download` y `GET ri/{clientGuidId}/zip`. Build Web.Api verde. Commit.

## Task P5: Frontend — quitar UI de subir (Paso 5)
**Files:** Modify `ZynstormECFPlatform-FrontEnd/components/certification/ri-step5.tsx`, `services/certification.service.ts`.
- [ ] Quitar la sección de subir/gestionar modelos y sus llamadas (`uploadRiModel`, `listRiModels`, `updateRiModel`, `deleteRiModel`, `riModelPreviewUrl`). Dejar la lista de comprobantes con Vista previa/Descargar por comprobante + Descargar ZIP. `npx tsc --noEmit` sin errores nuevos. Commit.

## Self-Review
- Cobertura: §2→P1/P2/P3, §4/§5→P3/P4/P5, §7→P1/P2. QR/mapper reusados.
- Consistencia: `EcfRiTemplateMapper.MapInvoice/MapPurchase` (P1/P2) consumidos por `RiPdfRenderer` (P3). View-models estables.
## Riesgo
- El port arrastra helpers/en“ums de EasyInvoice → copiar solo lo mínimo. Verificación visual (render+Read) por el controller al final.
