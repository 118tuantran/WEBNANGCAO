using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Data;
using WarehouseManagement.Services;
using WarehouseManagement.Domain;

namespace WarehouseManagement.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public sealed class AdminController(WarehouseDbContext db, InventoryService service) : ControllerBase
{
    [HttpGet("products")]
    public async Task<IActionResult> Products() => Ok(await service.GetProductsAsync());

    [HttpGet("users")]
    public async Task<IActionResult> Users() => Ok(await db.Users.AsNoTracking().OrderBy(x => x.Username).Select(x => new { x.Id, x.Username, x.Role, x.IsActive }).ToListAsync());

    [HttpGet("categories")]
    public async Task<IActionResult> Categories() => Ok(await service.GetCategoriesAsync());

    [HttpGet("warehouses")]
    public async Task<IActionResult> Warehouses() => Ok(await service.GetWarehousesAsync());

    [HttpGet("audit")]
    public async Task<IActionResult> Audit() => Ok(await db.AuditLogs.AsNoTracking().OrderByDescending(x => x.CreatedAt).Take(200).ToListAsync());

    [HttpPost("users")]
    public async Task<IActionResult> SaveUser(UserRequest request) => await Execute(() => service.SaveUserAsync(request.Id, request.Username, request.Password, request.Role, request.IsActive));

    [HttpPost("products")]
    public async Task<IActionResult> SaveProduct(ProductRequest request) => await Execute(() => service.SaveProductAsync(request.Id, request.Sku, request.Name, request.Unit, request.CategoryId, request.IsActive));

    [HttpPost("categories")]
    public async Task<IActionResult> SaveCategory(CategoryRequest request) => await Execute(() => service.SaveCategoryAsync(request.Id, request.Name, request.IsActive));

    [HttpPost("warehouses")]
    public async Task<IActionResult> SaveWarehouse(WarehouseRequest request) => await Execute(() => service.SaveWarehouseAsync(request.Id, request.Code, request.Name, request.IsActive));

    private async Task<IActionResult> Execute<T>(Func<Task<T>> action)
    {
        try { return Ok(await action()); }
        catch (InvalidOperationException e) { return BadRequest(new { message = e.Message }); }
        catch (DbUpdateException) { return Conflict(new { message = "Mã hoặc tên đã tồn tại." }); }
    }
}

public sealed record UserRequest(int? Id, string Username, string Password, UserRole Role, bool IsActive);
public sealed record ProductRequest(int? Id, string Sku, string Name, string Unit, int? CategoryId, bool IsActive);
public sealed record CategoryRequest(int? Id, string Name, bool IsActive);
public sealed record WarehouseRequest(int? Id, string Code, string Name, bool IsActive);
