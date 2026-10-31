using Swashbuckle.AspNetCore.SwaggerUI;
using VehicleExplorer.Api;
using VehicleExplorer.Application;
using VehicleExplorer.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddMemoryCache();
builder.Services.AddVehicleCatalog();
builder.Services.AddNhtsaClient(builder.Configuration);
builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalUi", policy =>
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

app.UseCors("LocalUi");
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Vehicle Explorer API");
    });
}

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.MapControllers();

app.Run();
