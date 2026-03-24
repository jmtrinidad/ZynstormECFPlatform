using Microsoft.EntityFrameworkCore;
using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Data.Extensions;
using ZynstormECFPlatform.Data.Services.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAutoMapper(c => { }, typeof(ZynstormECFPlatform.Mappings.MappingProfiles));

builder.Services.AddDbContextData(builder.Configuration.GetConnectionString("DefaultConnection")!);

builder.Services.AddDataServices();
//builder.Services.AddServices();

var app = builder.Build();

_ = SeedData(app);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Zynstorm ECF API v1");
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

static async Task SeedData(WebApplication app)
{
    using var scope = app.Services.CreateScope();

    var db = scope.ServiceProvider.GetRequiredService<IStorageContext>();
    await db.Database.MigrateAsync();

    //var service = scope.ServiceProvider.GetService<SeedDb>();

    //if (service is not null)
    //    await service.SeedAsync();
}