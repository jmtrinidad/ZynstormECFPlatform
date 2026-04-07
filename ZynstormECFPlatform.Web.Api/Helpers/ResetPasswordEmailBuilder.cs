using System.Text;

namespace ZynstormECFPlatform.Web.Api.Helpers
{
    public static class ResetPasswordEmailBuilder
    {
        public static string Build(string callbackUrl)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<div style='background-color: #f4f4f7; padding: 40px 20px; font-family: sans-serif;'>");
            sb.AppendLine("  <table align='center' width='100%' border='0' cellpadding='0' cellspacing='0' style='max-width: 500px; background-color: #ffffff; border-radius: 12px; box-shadow: 0 4px 12px rgba(0,0,0,0.1); overflow: hidden;'>");
            sb.AppendLine("    <tr>");
            sb.AppendLine("      <td style='padding: 40px; text-align: center;'>");
            sb.AppendLine("        <h2 style='color: #333; margin-bottom: 20px; font-size: 24px;'>Restablecer contraseña</h2>");
            sb.AppendLine("        <p style='color: #666; font-size: 16px; line-height: 1.5; margin-bottom: 30px;'>");
            sb.AppendLine("          Has solicitado restablecer tu contraseña. Haz clic en el botón de abajo para continuar.");
            sb.AppendLine("        </p>");
            sb.AppendLine("        <p style='color: #8a8fa3; font-size: 14px; line-height: 1.5; margin-bottom: 24px;'>");
            sb.AppendLine("          Este enlace será válido únicamente por 30 minutos.");
            sb.AppendLine("        </p>");
            sb.AppendLine($"        <a href='{callbackUrl}' style='display: inline-block; background-color: #5d59d6; color: #ffffff; padding: 14px 30px; border-radius: 25px; text-decoration: none; font-weight: bold; text-transform: uppercase; font-size: 14px; letter-spacing: 1px;'>");
            sb.AppendLine("          Restablecer contraseña");
            sb.AppendLine("        </a>");
            sb.AppendLine("        <p style='color: #999; font-size: 13px; margin-top: 40px; border-top: 1px solid #eeeeee; padding-top: 20px;'>");
            sb.AppendLine("          Si no solicitaste este cambio, puedes ignorar este correo con seguridad.");
            sb.AppendLine("          <br/><br/> Saludos,<br/><strong>Zynstorm Support</strong>");
            sb.AppendLine("        </p>");
            sb.AppendLine("      </td>");
            sb.AppendLine("    </tr>");
            sb.AppendLine("  </table>");
            sb.AppendLine("</div>");
            return sb.ToString();
        }
    }
}
