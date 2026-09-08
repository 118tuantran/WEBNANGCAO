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
    public async Task<ActionResult<List<ReportRow>>> InOutStock(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int? warehouseId,
        CancellationToken cancellationToken)
    {
        if (from.HasValue && to.HasValue && from.Value.Date > to.Value.Date)
            return BadRequest(new { message = "Ngày bắt đầu không được sau ngày kết thúc." });

        if (warehouseId is <= 0)
            return BadRequest(new { message = "Mã kho phải lớn hơn 0." });

        return Ok(await service.GetReportAsync(from, to, warehouseId, cancellationToken));
    }
}
