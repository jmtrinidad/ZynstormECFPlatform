using System;
using ZynstormECFPlatform.Abstractions.Services;

namespace ZynstormECFPlatform.Services;

public class EmailService : IEmailService
{
    public async Task SendApiKeyEmailAsync(string email, string apiKey, string secretKey)
    {
        // TODO: Configure SMTP or Email Provider (e.g., SendGrid, Mailtrap)
        // This is a skeleton implementation.

        var message = $@"
        <div style=""font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; max-width: 600px; margin: 0 auto; background-color: #f4f7f9; padding: 20px; border-radius: 8px;"">
            <div style=""background-color: #ffffff; padding: 40px; border-radius: 8px; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);"">
                <h1 style=""color: #2c3e50; text-align: center; margin-bottom: 30px;"">¡Bienvenido a Zynstorm ECF!</h1>
                <p style=""color: #34495e; font-size: 16px; line-height: 1.6;"">
                    Hola, se han generado con éxito tus credenciales de acceso para la plataforma <strong>Zynstorm ECF</strong>.
                    Utiliza estos datos para autenticar tus solicitudes a nuestra API.
                </p>

                <div style=""background-color: #f8f9fa; border-left: 4px solid #3498db; padding: 20px; margin: 30px 0;"">
                    <p style=""margin: 0; padding-bottom: 10px; font-weight: bold; color: #2c3e50;"">Tus Credenciales:</p>
                    <table style=""width: 100%; border-collapse: collapse;"">
                        <tr>
                            <td style=""padding: 8px 0; color: #7f8c8d; width: 100px;""><strong>API Key:</strong></td>
                            <td style=""padding: 8px 0; font-family: monospace; font-size: 14px; color: #34495e;"">{apiKey}</td>
                        </tr>
                        <tr>
                            <td style=""padding: 8px 0; color: #7f8c8d;""><strong>Secret Key:</strong></td>
                            <td style=""padding: 8px 0; font-family: monospace; font-size: 14px; color: #e74c3c;"">{secretKey}</td>
                        </tr>
                    </table>
                </div>

                <div style=""background-color: #fff3cd; border: 1px solid #ffeeba; color: #856404; padding: 15px; border-radius: 4px; font-size: 14px;"">
                    <strong>⚠️ Seguridad Importante:</strong><br>
                    Por favor, guarda el <strong>Secret Key</strong> en un lugar seguro. Por motivos de seguridad, no podremos mostrártelo de nuevo.
                </div>

                <p style=""margin-top: 30px; font-size: 14px; color: #7f8c8d; text-align: center;"">
                    Si no has solicitado estas credenciales, por favor contacta con soporte de inmediato.
                </p>
            </div>
            <div style=""text-align: center; margin-top: 20px; color: #95a5a6; font-size: 12px;"">
                &copy; {DateTime.UtcNow.Year} Zynstorm ECF Platform. Todos los derechos reservados.
            </div>
        </div>
";

        // Implementation for sending email goes here
        await Task.CompletedTask;
    }
}