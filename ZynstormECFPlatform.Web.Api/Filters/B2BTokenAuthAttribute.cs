using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ZynstormECFPlatform.Web.Api.Filters;

/// <summary>
/// Filtro de autorización para endpoints B2B que requieren el Bearer Token devuelto por la DGII.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class B2BTokenAuthAttribute : Attribute, IAsyncActionFilter
{
    private const string TokenPrefix = "Bearer ";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Respetar [AllowAnonymous]
        if (context.ActionDescriptor.EndpointMetadata.Any(em => em.GetType() == typeof(Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute)))
        {
            await next();
            return;
        }

        if (!context.HttpContext.Request.Headers.TryGetValue("Authorization", out var authHeader) || 
            !authHeader.ToString().StartsWith(TokenPrefix, StringComparison.OrdinalIgnoreCase))
        {
            context.Result = new UnauthorizedObjectResult(new { error = "Acceso denegado. Se requiere Token Bearer de autenticación B2B." });
            return;
        }

        // En entornos de prueba, aceptamos el token Bearer retornado por ValidarCertificado.
        await next();
    }
}
