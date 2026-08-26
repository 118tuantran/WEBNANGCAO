using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Data;
using WarehouseManagement.Services;

namespace WarehouseManagement.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public sealed class AdminController(WarehouseDbContext db, InventoryService service) : ControllerBase
{
    [HttpGet("products")]
    public async Task<IActionResult> Products() => Ok(await service.GetProductsAsync());

    [HttpGet("warehouses")]
    [AllowAnonymous]
    public async Task<IActionResult> Warehouses() => Ok(await service.GetWarehousesAsync());

    [HttpGet("audit")]
    public async Task<IActionResult> Audit() => Ok(await db.AuditLogs.AsNoTracking().OrderByDescending(x => x.CreatedAt).Take(200).ToListAsync());
}
