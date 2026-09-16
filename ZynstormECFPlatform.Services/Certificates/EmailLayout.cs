namespace ZynstormECFPlatform.Services.Certificates;

/// <summary>Marco HTML compartido por los correos de aviso de la plataforma.</summary>
public static class EmailLayout
{
    public static string Wrap(string title, string innerHtml) => $@"
        <div style=""font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; max-width: 640px; margin: 0 auto; background-color: #f4f7f9; padding: 20px; border-radius: 8px;"">
            <div style=""background-color: #ffffff; padding: 32px; border-radius: 8px; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);"">
                <h1 style=""color: #2c3e50; font-size: 22px; margin: 0 0 20px;"">{title}</h1>
                {innerHtml}
            </div>
            <div style=""text-align: center; margin-top: 20px; color: #95a5a6; font-size: 12px;"">
                &copy; {DateTime.UtcNow.Year} Zynstorm ECF Platform. Todos los derechos reservados.
            </div>
        </div>";
}
