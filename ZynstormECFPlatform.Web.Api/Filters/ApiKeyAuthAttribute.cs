using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ZynstormECFPlatform.Abstractions.DataServices;

namespace ZynstormECFPlatform.Web.Api.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ApiKeyAuthAttribute : Attribute, IAsyncActionFilter
{
    private const string ApiKeyHeaderName = "X-Api-Key";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Ignorar si el endpoint tiene [AllowAnonymous] (Por ejemplo, FeController)
        if (context.ActionDescriptor.EndpointMetadata.Any(em => em.GetType() == typeof(Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute)))
        {
            await next();
            return;
        }

        // Si la petición ya fue autenticada por la Web App (usando el Bearer JWT), permitir acceso
        if (context.HttpContext.User.Identity != null && context.HttpContext.User.Identity.IsAuthenticated)
        {
            await next();
            return;
        }

        if (!context.HttpContext.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey))
        {
            context.Result = new UnauthorizedObjectResult(new { error = "Acceso denegado: Se requiere un ApiKey válido en el header 'X-Api-Key'." });
            return;
        }

        var apiKeyService = context.HttpContext.RequestServices.GetRequiredService<IApiKeyService>();
        
        // Buscar el ApiKey en la base de datos (StatusId = 1 significa activo generalmente)
        var apiKey = await apiKeyService.GetByAsync(x => x.Apikey == extractedApiKey.ToString() && x.StatusId == 1);
        
        if (apiKey == null) 
        {
            context.Result = new UnauthorizedObjectResult(new { error = "Acceso denegado: El ApiKey proporcionado es inválido o se encuentra inactivo." });
            return;
        }

        // Si la validación es exitosa, podemos inyectar los datos del cliente en el HttpContext 
        // para que los controladores puedan usarlos libremente.
        context.HttpContext.Items["ClientId"] = apiKey.ClientId;
        context.HttpContext.Items["ClientBrancheId"] = apiKey.ClientBrancheId;

        await next();
    }
}
