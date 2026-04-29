using Microsoft.AspNetCore.Authentication;
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
using Hangfire;
using Hangfire.PostgreSql;
using ZynstormECFPlatform.Data;

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
builder.Services.AddSwaggerGen();

builder.Services.AddAutoMapper(c => { }, typeof(ZynstormECFPlatform.Mappings.MappingProfiles));

builder.Services.AddDbContextData(builder.Configuration.GetConnectionString("DefaultConnection")!);

builder.Services.AddDataServices();
builder.Services.AddServices();

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
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "Zynstorm ECF_Platform Api", Version = "v1" });
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
        }).AddIdentityCookies();
}
else
{
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "EasyInvoice API", Version = "v1" });
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

_ = SeedData(app);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Zynstorm ECF API v1");
        options.EnablePersistAuthorization();
    });
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard();

app.MapControllers();

app.Run();

static async Task SeedData(WebApplication app)
{
    using var scope = app.Services.CreateScope();

    var db = scope.ServiceProvider.GetRequiredService<StorageContext>();
    db.Database.Migrate();

    //var service = scope.ServiceProvider.GetService<SeedDb>();

    //if (service is not null)
    //    await service.SeedAsync();
}