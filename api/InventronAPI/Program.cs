using InventronAPI.Data;
using InventronAPI.Infrastructure;
using InventronAPI.Services.Implementations;
using InventronAPI.Services.Interfaces;
using InventronAPI.Services.Utils;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantProvider, HttpTenantProvider>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("LocalConnection")));

builder.Services.AddAutoMapper(cfg => cfg.AddMaps(typeof(ServiceMapper).Assembly));

builder.Services.AddScoped<IItemsService, ItemsService>();

builder.Services.AddCors(options =>
{
	options.AddPolicy("Frontend", policy =>
		policy.WithOrigins("http://localhost:5173")
		.AllowAnyHeader()
		.AllowAnyMethod());
});


var app = builder.Build();

app.UseExceptionHandler();

// Enabled by docker-compose so a new database container gets its schema on startup.
if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
	using var scope = app.Services.CreateScope();
	scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
	app.MapScalarApiReference();
}

app.UseCors("Frontend");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
