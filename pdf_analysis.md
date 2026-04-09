# Análisis: PDF Técnico DGII + Namespace en `<ECF>`

## Parte 1: ¿De dónde viene `xmlns="http://dgii.gov.do/ecf" version="1.0"`?

> [!IMPORTANT]
> Este atributo **NO debe ser agregado por nuestro generador**. Proviene del proceso de firma digital XML-DSig y es introducido por la herramienta/librería de firma, no por el XML generado.

### Evidencia en el XSD oficial

El XSD del DGII define el elemento raíz **sin ningún namespace ni atributos adicionales**:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema">
  <xs:element name="ECF">
    ...
  </xs:element>
</xs:schema>
```

- El `<xs:schema>` **no tiene `targetNamespace`** — es decir, el XSD no define ningún namespace para los elementos del e-CF.
- El elemento raíz se llama `ECF` (mayúsculas), **no** `eCF`.
- No hay ningún atributo `version` en la definición del elemento raíz.

### Conclusión sobre el namespace

El `xmlns="http://dgii.gov.do/ecf" version="1.0"` que observas en el XML firmado es añadido por la **librería de firma digital** durante el proceso de canonicalización y firma XML-DSig (SHA-256). Esto es un comportamiento común en implementaciones de firma XML donde el software de firma puede:

1. Cambiar el nombre del elemento raíz de `ECF` a `eCF`.
2. Agregar el atributo `xmlns` con el namespace de la DGII.
3. Agregar un atributo `version="1.0"`.

**El XML que nosotros generamos es correcto** — el elemento raíz `<ECF>` sin namespace ni atributos es lo que el XSD espera. La transformación ocurre después, en el paso de firma.

> [!NOTE]
> Cuando implementes el servicio de firma, deberás verificar si la librería que uses transforma el elemento raíz. Si lo hace, asegúrate de que la validación XSD se realice **antes** de la firma (con el XML sin firmar que generamos), no después.

---

## Parte 2: Lo que dice el PDF — Puntos Técnicos Clave

### Proceso de Firma Digital (página 60)

El PDF establece los siguientes requisitos para el firmado de XML:

- ✅ **Protocolo:** SHA-256
- ✅ **Campo SN del certificado:** debe corresponder al RNC, Cédula o Pasaporte del propietario
- ✅ **Preservación de espacios:** `preserveWhitespace = false` — firmar sin preservar espacios en blanco
- ✅ **Inmutabilidad:** Una vez firmado, el XML no puede ser alterado bajo ninguna circunstancia

### Formato del nombre del archivo XML (página 59)

```
RNCEmisor + eNCF → Ejemplo: 101672919E3100000001.xml
```

| Formato | Nombre de archivo |
|---------|------------------|
| e-CF | `RNCEmisor + eNCF` |
| Aprobación Comercial | `RNCComprador + eNCF` |
| Acuse de Recibo | `RNCComprador + eNCF` |
| Resumen Factura Consumo (32) | `RNCEmisor + eNCF` |

### Tags vacíos (página 59)

> [!WARNING]
> **"No deberá incluirse en los XML tags vacíos. Todo tag que no vaya a ser utilizado debe excluirse de los eCF o de lo contrario provocará un rechazo."**

Esto confirma que nuestros `ShouldSerialize*()` son obligatorios y están bien implementados.

### URLs de los servicios DGII

| Ambiente | Servicio | URL |
|---------|---------|-----|
| Pre-certificación | Recepción e-CF | `https://ecf.dgii.gov.do/testecf/recepcion/api/facturaselectronicas` |
| Producción | Recepción e-CF | `https://ecf.dgii.gov.do/ecf/recepcion` |
| Pre-certificación | Resumen FC <250k | `https://fc.dgii.gov.do/testecf/recepcionfc/api/recepcion/ecf` |
| Producción | Resumen FC <250k | `https://fc.dgii.gov.do/ecf/recepcionfc` |
| Emisor-Receptor | Semilla auth | `https://ecf.dgii.gov.do/testecf/emisorreceptor/fe/autenticacion/api/semilla` |
| Emisor-Receptor | Recepción | `https://ecf.dgii.gov.do/testecf/emisorreceptor/fe/recepcion/api/ecf` |

### Tipos de comprobantes que NO se envían a otro contribuyente

Los tipos **32, 41, 43, 45, 46 y 47** solo se envían a la DGII, **no** al receptor (contribuyente comprador):

| Tipo | Envía a DGII | Envía al receptor |
|------|-------------|------------------|
| 31 | ✅ | ✅ |
| 32 | ✅ (resumen si <250k, completo si ≥250k) | ❌ |
| 33 | ✅ | ✅ |
| 34 | ✅ | ✅ |
| 41 | ✅ | ❌ |
| 43 | ✅ | ❌ |
| 44 | ✅ | ✅ |
| 45 | ✅ | ❌ |
| 46 | ✅ | ❌ |
| 47 | ✅ | ❌ |

### La respuesta del DGII al recibir un e-CF (Acuse de Recibo)

```xml
<?xml version="1.0" encoding="utf-8"?>
<ARECF xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
       xmlns:xsd="http://www.w3.org/2001/XMLSchema">
  <DetalleAcusedeRecibo>
    <Version>1.0</Version>
    <RNCEmisor>131880600</RNCEmisor>
    <RNCComprador>132880600</RNCComprador>
    <eNCF>E310000000001</eNCF>
    <Estado>0</Estado>
    <FechaHoraAcuseRecibo>17-12-2020 11:19:06</FechaHoraAcuseRecibo>
  </DetalleAcusedeRecibo>
</ARECF>
```

El formato de `FechaHoraAcuseRecibo` es `dd-MM-yyyy HH:mm:ss` — **igual al formato que usamos en `FechaHoraFirma`**. ✅

### Respuesta del servicio de recepción (trackId)

```json
{
  "trackId": "string",
  "error": "string",
  "mensaje": "string"
}
```

El `trackId` es el identificador único asignado por la DGII a cada e-CF recibido. Esto ya está modelado en nuestra entidad `EcfTransmission.TrackId`.

### Método de autenticación (Bearer Token)

Todo envío requiere un header `Authorization: bearer {token}` obtenido mediante:
1. GET `/fe/autenticacion/api/semilla` → obtiene XML con semilla
2. Firmar digitalmente el XML semilla con el certificado
3. POST `/fe/autenticacion/api/validacioncertificado` → retorna el token JWT (expira en ~1 hora)

---

## Parte 3: Resumen de Acciones

### ✅ No cambiar — está correcto
- El XML raíz `<ECF>` sin namespace ni atributos — el XSD no los define
- El formato `FechaHoraFirma` en `dd-MM-yyyy HH:mm:ss`
- Los `ShouldSerialize` para evitar tags vacíos

### 🔴 Pendiente — lo que hay que implementar
1. **Servicio de firma digital** (SHA-256, preserveWhitespace=false)
2. **Servicio de autenticación** (semilla → firma → token JWT)
3. **Servicio de envío** a la DGII (con header Authorization Bearer)
4. **Lógica de enrutamiento de envío**: tipos 31, 33, 34, 44 van también a receptor; los demás solo a DGII
5. **Resumen de Factura de Consumo** para tipo 32 con monto <250,000 → endpoint diferente
6. **Persistencia del trackId** en `EcfTransmission` cuando la DGII responde

### 🟡 El namespace que ves en el eCF firmado
El `xmlns="http://dgii.gov.do/ecf"` y `version="1.0"` que aparecen en el XML son **transformaciones que hace la librería de firma digital**. Cuando implementes la firma, úsalos correctamente según lo que espere esa librería, pero no los agregues en el generador de XML sin firmar.
