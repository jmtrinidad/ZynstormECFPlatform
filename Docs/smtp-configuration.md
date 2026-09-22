# Configuración SMTP

La contraseña SMTP no debe guardarse en `appsettings*.json` ni versionarse. La aplicación lee la clave desde la configuración estándar de ASP.NET Core.

## Desarrollo local

Desde la raíz del repositorio:

```powershell
dotnet user-secrets set "AppSettings:SmtpAppPassword" "<contraseña-de-aplicación>" --project ZynstormECFPlatform.Web.Api
```

## Staging y producción

Configurar el secreto en el entorno del servicio o en el gestor de secretos del despliegue:

```text
AppSettings__SmtpAppPassword=<contraseña-de-aplicación>
```

Para Gmail debe ser una contraseña de aplicación vigente de la misma cuenta indicada en `AppSettings:SmtpUsername`. No se debe usar la contraseña normal de la cuenta.

Si Gmail responde `535 5.7.8 Username and Password not accepted` o `5.7.0 Authentication Required`, generar una nueva contraseña de aplicación, reemplazar el secreto del entorno y reiniciar la instancia. Cambiar la contraseña de la cuenta de Google puede revocar las contraseñas de aplicación existentes.
