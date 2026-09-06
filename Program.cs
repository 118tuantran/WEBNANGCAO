using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using QuestPDF.Infrastructure;
using System.Security.Claims;
using WarehouseManagement.Data;
using WarehouseManagement.Domain;

var builder = WebApplication.CreateBuilder(args);
QuestPDF.Settings.License = LicenseType.Community;

builder.Services.AddDbContext<WarehouseManagement.Data.WarehouseDbContext>(options =>
	options.UseSqlite(builder.Configuration.GetConnectionString("Warehouse") ?? "Data Source=warehouse.db"));
builder.Services.AddAuthentication("WarehouseCookie")
	.AddCookie("WarehouseCookie", options =>
	{
		options.Cookie.Name = "warehouse_session";
		options.LoginPath = "/api/auth/login";
		options.Events.OnRedirectToLogin = context =>
		{
			using var scope = context.HttpContext.RequestServices.CreateScope();
			var db = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();
			db.AuditLogs.Add(new AuditLog { UserId = 0, Action = "ACCESS_UNAUTHORIZED", EntityType = "Security", Metadata = context.Request.Path });
			db.SaveChanges();
			context.Response.StatusCode = StatusCodes.Status401Unauthorized;
			return Task.CompletedTask;
		};
		options.Events.OnRedirectToAccessDenied = context =>
		{
			using var scope = context.HttpContext.RequestServices.CreateScope();
			var db = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();
			var userId = int.TryParse(context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), out var parsed) ? parsed : 0;
			db.AuditLogs.Add(new AuditLog { UserId = userId, Action = "ACCESS_FORBIDDEN", EntityType = "Security", Metadata = context.Request.Path });
			db.SaveChanges();
			context.Response.StatusCode = StatusCodes.Status403Forbidden;
			return Task.CompletedTask;
		};
	});
builder.Services.AddAuthorization();
builder.Services.AddScoped<WarehouseManagement.Services.InventoryService>();

builder.Services.AddControllers().AddJsonOptions(options =>
	options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
	await scope.ServiceProvider.GetRequiredService<WarehouseManagement.Data.WarehouseDbContext>().Database.EnsureCreatedAsync();
}

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
