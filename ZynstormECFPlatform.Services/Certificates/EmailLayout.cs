namespace ZynstormECFPlatform.Services.Certificates;

/// <summary>Marco HTML compartido por los correos de aviso de la plataforma.</summary>
public static class EmailLayout
{
    public static string Wrap(
        string title,
        string innerHtml,
        string? badgeText = null,
        string? badgeBg = null,
        string? badgeColor = null)
    {
        var badgeHtml = string.IsNullOrWhiteSpace(badgeText)
            ? string.Empty
            : $@"<span style=""display: inline-block; padding: 4px 10px; font-size: 11px; font-weight: 700; text-transform: uppercase; letter-spacing: 0.5px; border-radius: 9999px; background-color: {badgeBg ?? "#e0f2fe"}; color: {badgeColor ?? "#0369a1"};"">{badgeText}</span>";

        return $@"
        <div style=""background-color: #f1f5f9; padding: 24px 0; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;"">
            <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"">
                <tr>
                    <td align=""center"">
                        <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""max-width: 600px; background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.07), 0 2px 4px -2px rgba(0, 0, 0, 0.05); border: 1px solid #e2e8f0; text-align: left;"">
                            <!-- Header Bar -->
                            <tr>
                                <td style=""background: linear-gradient(135deg, #0f172a 0%, #1e293b 100%); padding: 22px 28px;"">
                                    <table width=""100%"" cellpadding=""0"" cellspacing=""0"">
                                        <tr>
                                            <td style=""vertical-align: middle;"">
                                                <table cellpadding=""0"" cellspacing=""0"">
                                                    <tr>
                                                        <td style=""vertical-align: middle; padding-right: 14px;"">
                                                            <img src=""{ZynstormBrand.IconDataUri}"" width=""42"" height=""42"" alt=""Zynstorm"" style=""display: block; width: 42px; height: 42px; border: 0;"" />
                                                        </td>
                                                        <td style=""vertical-align: middle;"">
                                                            <div style=""font-size: 22px; font-weight: 800; letter-spacing: -0.5px; line-height: 1.1;"">
                                                                {ZynstormBrand.NameHtml}
                                                            </div>
                                                            <div style=""font-size: 11px; color: #94a3b8; letter-spacing: 0.3px; margin-top: 3px; font-weight: 400;"">
                                                                {ZynstormBrand.Slogan}
                                                            </div>
                                                        </td>
                                                    </tr>
                                                </table>
                                            </td>
                                            <td align=""right"" style=""vertical-align: middle;"">
                                                {badgeHtml}
                                            </td>
                                        </tr>
                                    </table>
                                </td>
                            </tr>
                            <!-- Content Body -->
                            <tr>
                                <td style=""padding: 32px 32px 28px 32px;"">
                                    <h2 style=""margin: 0 0 16px 0; font-size: 20px; font-weight: 700; color: #0f172a; line-height: 1.3;"">{title}</h2>
                                    {innerHtml}
                                </td>
                            </tr>
                            <!-- Footer -->
                            <tr>
                                <td style=""background-color: #f8fafc; border-top: 1px solid #e2e8f0; padding: 20px 32px; text-align: center;"">
                                    <p style=""margin: 0; font-size: 12px; color: #94a3b8; line-height: 1.6;"">
                                        Este es un mensaje automático de control emitido por <strong><span style=""color: #7c3aed;"">Zyn</span><span style=""color: #0284c7;"">storm</span></strong>.<br>
                                        <span style=""color: #64748b; font-style: italic;"">{ZynstormBrand.Slogan}</span><br>
                                        &copy; {DateTime.UtcNow.Year} {ZynstormBrand.CompanyName}. Todos los derechos reservados.
                                    </p>
                                </td>
                            </tr>
                        </table>
                    </td>
                </tr>
            </table>
        </div>";
    }
}
