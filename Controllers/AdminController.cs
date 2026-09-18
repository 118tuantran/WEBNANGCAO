using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using WarehouseManagement.Data;
using WarehouseManagement.Services;
using WarehouseManagement.Domain;

namespace WarehouseManagement.Controllers;

[ApiController]
[Route("api/admin")]
public sealed class AdminController(WarehouseDbContext db, InventoryService service) : ControllerBase
{
    [HttpGet("products")]
    [Authorize(Roles = "WarehouseManager")]
    public async Task<IActionResult> Products() => Ok(await service.GetProductsAsync());

    [HttpGet("users")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Users() => Ok(await db.Users.AsNoTracking().OrderBy(x => x.Username).Select(x => new { x.Id, x.Username, x.Role, x.IsActive }).ToListAsync());

    [HttpGet("categories")]
    [Authorize(Roles = "WarehouseManager")]
    public async Task<IActionResult> Categories() => Ok(await service.GetCategoriesAsync());

    [HttpGet("warehouses")]
    [Authorize(Roles = "WarehouseManager")]
    public async Task<IActionResult> Warehouses() => Ok(await service.GetWarehousesAsync());

    [HttpGet("suppliers")]
    [Authorize(Roles = "WarehouseManager")]
    public async Task<IActionResult> Suppliers() => Ok(await service.GetSuppliersAsync());

    [HttpGet("audit")]
    [Authorize(Roles = "Admin,WarehouseManager")]
    public async Task<IActionResult> Audit() => Ok(await db.AuditLogs.AsNoTracking().OrderByDescending(x => x.CreatedAt).Take(200).ToListAsync());

    [HttpPost("users")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SaveUser(UserRequest request) => await Execute(() => service.SaveUserAsync(request.Id, request.Username, request.Password, request.Role, request.IsActive));

    [HttpPost("products")]
    [Authorize(Roles = "WarehouseManager")]
    public async Task<IActionResult> SaveProduct(ProductRequest request) => await Execute(() => service.SaveProductAsync(request.Id, request.Sku, request.Name, request.Unit, request.CategoryId, request.IsActive));

    [HttpPost("categories")]
    [Authorize(Roles = "WarehouseManager")]
    public async Task<IActionResult> SaveCategory(CategoryRequest request) => await Execute(() => service.SaveCategoryAsync(request.Id, request.Name, request.IsActive));

    [HttpPost("warehouses")]
    [Authorize(Roles = "WarehouseManager")]
    public async Task<IActionResult> SaveWarehouse(WarehouseRequest request) => await Execute(() => service.SaveWarehouseAsync(request.Id, request.Code, request.Name, request.IsActive));

    [HttpPost("suppliers")]
    [Authorize(Roles = "WarehouseManager")]
    public async Task<IActionResult> SaveSupplier(SupplierRequest request) => await Execute(() => service.SaveSupplierAsync(request.Id, request.Code, request.Name, request.Phone, request.IsActive));

    private async Task<IActionResult> Execute<T>(Func<Task<T>> action)
    {
        try { return Ok(await action()); }
        catch (InvalidOperationException e) { return BadRequest(new { message = e.Message }); }
        catch (DbUpdateException) { return Conflict(new { message = "Mã hoặc tên đã tồn tại." }); }
    }
}

public sealed record UserRequest(int? Id, [Required, StringLength(100)] string Username, string Password, UserRole Role, bool IsActive);
public sealed record ProductRequest(int? Id, [Required, StringLength(100)] string Sku, [Required] string Name, [Required] string Unit, int? CategoryId, bool IsActive);
public sealed record CategoryRequest(int? Id, [Required] string Name, bool IsActive);
public sealed record WarehouseRequest(int? Id, [Required] string Code, [Required] string Name, bool IsActive);
public sealed record SupplierRequest(int? Id, [Required] string Code, [Required] string Name, string? Phone, bool IsActive);
