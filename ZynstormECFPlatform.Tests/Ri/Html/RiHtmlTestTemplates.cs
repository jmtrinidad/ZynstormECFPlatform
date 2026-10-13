namespace ZynstormECFPlatform.Tests.Ri.Html;

/// <summary>Reproduce el formato de referencia entregado por el usuario (Royaltrix Software).</summary>
public static class RiHtmlTestTemplates
{
    public const string Reference = """
<!DOCTYPE html>
<html><head><meta charset="utf-8"><title>Factura</title></head>
<body style="font-family:Arial;width:300px">
  <div id="logo" style="border:1px dashed #000;height:60px;text-align:center">{{EmisorLogo}}</div>
  <div style="text-align:center">{{EmisorDireccion}}</div>
  <div style="text-align:center"><b>RNC: {{EmisorRNC}}</b></div>
  <div style="text-align:center"><b>Tel.: {{EmisorTelefono}}</b></div>
  <h2><u>FACTURA</u></h2>
  <div style="text-align:center"><b>{{TipoComprobante}}</b></div>
  <div class="row"><span>eNCF:</span><span>{{eNCF}}</span></div>
  <div class="row"><span>VALIDO HASTA:</span><span>{{FechaVencimiento}}</span></div>
  <div class="row"><span>No. Factura:</span><b>{{NoFactura}}</b></div>
  <div class="row"><span>Fecha:</span><span>{{Fecha}}</span></div>
  <div class="row"><span>Cédula:</span><span>{{Cedula}}</span></div>
  <div class="row"><span>Nombre:</span><b>{{Nombre}}</b></div>
  <div class="row"><span>Sexo:</span><span>{{Sexo}}</span></div>
  <div class="row"><span>Fecha Nac.:</span><span>{{FechaNac}}</span></div>
  <div class="row"><span>Edad:</span><span>{{Edad}}</span></div>
  <div class="row"><span>Seg. Méd.:</span><span>{{SegMed}}</span></div>
  <div class="row"><span>NSS:</span><span>{{NSS}}</span></div>
  <div class="row"><span>Forma Pago:</span><b>{{FormaPago}}</b></div>
  <div class="row"><span>Devolución:</span><span>{{Devolucion}}</span></div>
  <div class="row"><span>Médico:</span><span>{{Medico}}</span></div>
  <table id="items" style="width:100%">
    <thead><tr><th>Descripción</th><th>Monto</th></tr></thead>
    <tbody><tr><td>{{Descripcion}}</td><td>{{Monto}}</td></tr></tbody>
  </table>
  <div class="row"><b>Total:</b><b>{{Total}}</b></div>
  <div class="row"><b>Total Descuento:</b><b>{{TotalDescuento}}</b></div>
  <div class="row"><b>Monto Pagado:</b><b>{{MontoPagado}}</b></div>
  <div class="row"><b>Restante:</b><b>{{Restante}}</b></div>
  <div id="qr" style="border:1px dashed #000;width:110px;height:110px;margin:auto">{{QR}}</div>
  <div style="text-align:center"><b>Codigo seguridad:</b> {{CodigoSeguridad}}</div>
  <div style="text-align:center"><b>Fecha firma digital:</b> {{FechaFirma}}</div>
  <div style="text-align:center"><b>REPRESENTACION IMPRESA DEL e-CF</b></div>
</body></html>
""";
}
