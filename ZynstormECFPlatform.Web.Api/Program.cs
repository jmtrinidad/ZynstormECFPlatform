using Microsoft.AspNetCore.Authentication;
using Asp.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Core;
using ZynstormECFPlatform.Data.Extensions;
using ZynstormECFPlatform.Data.Services.Extensions;
using ZynstormECFPlatform.Web.Api.Handlers;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using ZynstormECFPlatform.Services.Extensions;
using ZynstormECFPlatform.Web.Api.Converters;
using ZynstormECFPlatform.Abstractions.DataServices;
using Hangfire;
using Hangfire.PostgreSql;
using ZynstormECFPlatform.Data;
using ZynstormECFPlatform.Data.Seeds;
using ZynstormECFPlatform.Common.Hubs;

// Program.cs
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new DrDateTimeConverter());
        options.JsonSerializerOptions.Converters.Add(new DrNullableDateTimeConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSignalR();

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
}).AddMvc().AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddAutoMapper(c => { }, typeof(ZynstormECFPlatform.Mappings.MappingProfiles));

builder.Services.AddDbContextData(builder.Configuration.GetConnectionString("DefaultConnection")!);

builder.Services.AddDataServices();
builder.Services.AddServices();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ZynstormECFPlatform.Abstractions.Services.ICurrentUserService, ZynstormECFPlatform.Web.Api.Services.CurrentUserService>();

// Hangfire configuration
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(options =>
    {
        options.UseNpgsqlConnection(builder.Configuration.GetConnectionString("HangfireConnection"));
    }));

builder.Services.AddHangfireServer();

builder.Services.Configure<AppSettings>(options => builder.Configuration.GetSection("AppSettings").Bind(options));

var appSettingsSection = builder.Configuration.GetSection("AppSettings");
var appSettings = appSettingsSection.Get<AppSettings>() ?? throw new InvalidOperationException("Missing AppSettings configuration.");

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "Zynstorm ECF Platform API", Version = "v1" });
        c.ResolveConflictingActions(r => r.First());
        c.AddSecurityDefinition("Swagger", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "Basic",
            In = ParameterLocation.Header,
            Description = "Basic Authorization header."
        });
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Swagger"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    builder.Services.AddAuthentication("BasicAuthentication")
        .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("BasicAuthentication", null)
        .AddJwtBearer(x =>
        {
            x.RequireHttpsMetadata = false;
            x.SaveToken = true;
            x.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(appSettings!.Secret)),
            };
            x.Events = new JwtBearerEvents
            {
                // Si no viene el header Authorization, toma el JWT de la cookie httpOnly.
                OnMessageReceived = context =>
                {
                    if (string.IsNullOrEmpty(context.Token))
                    {
                        context.Token = ZynstormECFPlatform.Web.Api.Helpers.AuthCookie.ReadToken(context.Request);
                    }
                    return Task.CompletedTask;
                },
                OnTokenValidated = async context =>
                {
                    var accountService = context.HttpContext.RequestServices.GetRequiredService<IAccountService>();
                    var userId = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    if (string.IsNullOrEmpty(userId))
                    {
                        context.Fail("User ID claim is missing.");
                        return;
                    }
                    var user = await accountService.GetUserByIdAsync(userId).ConfigureAwait(false);
                    if (user == null || !user.IsActive || user.IsDeleted)
                    {
                        context.Fail("User does not exist, is inactive or has been deleted.");
                    }
                }
            };
        }).AddIdentityCookies();
}
else
{
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "Zynstorm ECF Platform API", Version = "v1" });
        c.ResolveConflictingActions(r => r.First());
    });

    builder.Services.AddAuthentication(x =>
    {
        x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
     .AddJwtBearer(x =>
     {
         x.RequireHttpsMetadata = false;
         x.SaveToken = true;
         x.TokenValidationParameters = new TokenValidationParameters
         {
             ValidateIssuerSigningKey = true,
             ValidateIssuer = false,
             ValidateAudience = false,
             ValidateLifetime = true,
             ClockSkew = TimeSpan.Zero,
             IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(appSettings.Secret)),
         };
         x.Events = new JwtBearerEvents
         {
             // Si no viene el header Authorization, toma el JWT de la cookie httpOnly.
             OnMessageReceived = context =>
             {
                 if (string.IsNullOrEmpty(context.Token))
                 {
                     context.Token = ZynstormECFPlatform.Web.Api.Helpers.AuthCookie.ReadToken(context.Request);
                 }
                 return Task.CompletedTask;
             },
             OnTokenValidated = async context =>
             {
                 var accountService = context.HttpContext.RequestServices.GetRequiredService<IAccountService>();
                 var userId = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                 if (string.IsNullOrEmpty(userId))
                 {
                     context.Fail("User ID claim is missing.");
                     return;
                 }
                 var user = await accountService.GetUserByIdAsync(userId).ConfigureAwait(false);
                 if (user == null || !user.IsActive || user.IsDeleted)
                 {
                     context.Fail("User does not exist, is inactive or has been deleted.");
                 }
             }
         };
     }).AddIdentityCookies();
}

builder.Services.AddCors(c =>
{
    c.AddPolicy("corsGlobalPolicy", builder =>
        builder.SetIsOriginAllowed(origin => true) // Permite peticiones desde CUALQUIER URL
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials());

    c.AddPolicy("corsPolicy", builder =>
        builder.SetIsOriginAllowed(origin => true) // Permite peticiones desde CUALQUIER URL
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials());
});

var app = builder.Build();

await SeedData(app);

// Security headers (defensa en profundidad). Se agregan a todas las respuestas.
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next().ConfigureAwait(false);
});

// Configure the HTTP request pipeline.
// Temporalmente expuesto en producción para pruebas
// if (app.Environment.IsDevelopment())
// {
app.UseSwagger();

app.UseSwaggerUI(options =>
{
    // Se utiliza ruta relativa './v1/swagger.json' para evitar problemas con reverse proxies (como Dokploy/Nginx)
    options.SwaggerEndpoint("./v1/swagger.json", "Zynstorm ECF Platform API v1");
    options.EnablePersistAuthorization();
});
// }

app.UseWebSockets();
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseCors("corsGlobalPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard();

// Register Automatic Reports Recurring Jobs
RecurringJob.AddOrUpdate<ZynstormECFPlatform.Services.Jobs.AutomaticReportsJob>(
    "DailyReportJob",
    job => job.ExecuteDailyReportAsync(default),
    Cron.Daily(21, 0), // 9:00 PM everyday in DR
    new RecurringJobOptions { TimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Santo_Domingo") }
);

RecurringJob.AddOrUpdate<ZynstormECFPlatform.Services.Jobs.AutomaticReportsJob>(
    "WeeklyReportJob",
    job => job.ExecuteWeeklyReportAsync(default),
    Cron.Weekly(DayOfWeek.Friday, 18, 0), // 6:00 PM every Friday in DR
    new RecurringJobOptions { TimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Santo_Domingo") }
);

RecurringJob.AddOrUpdate<ZynstormECFPlatform.Services.Jobs.ReceivedB2BMessagesCleanupJob>(
    "ReceivedB2BMessagesCleanup",
    job => job.CleanupOldMessagesAsync(default),
    Cron.Daily(3, 0), // 3:00 AM everyday
    new RecurringJobOptions { TimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Santo_Domingo") }
);

app.MapControllers();
app.MapHub<CertificationHub>("/hubs/certification");

app.Run();

static async Task SeedData(WebApplication app)
{
    using var scope = app.Services.CreateScope();

    var db = scope.ServiceProvider.GetRequiredService<StorageContext>();
    db.Database.Migrate();

    var service = scope.ServiceProvider.GetService<UserAndRols>();

    if (service is not null)
        await service.SeedAsync();
}