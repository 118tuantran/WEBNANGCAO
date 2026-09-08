using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseManagement.Services;

namespace WarehouseManagement.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Roles = "WarehouseManager")]
public sealed class ReportController(InventoryService service) : ControllerBase
{
    [HttpGet("in-out-stock")]
    public async Task<IActionResult> InOutStock([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int? warehouseId) => Ok(await service.GetReportAsync(from, to, warehouseId));
}
