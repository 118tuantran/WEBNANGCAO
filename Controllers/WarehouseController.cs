using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseManagement.Domain;
using WarehouseManagement.Services;

namespace WarehouseManagement.Controllers;

[ApiController]
[Route("api/warehouse")]
[Authorize]
public sealed class WarehouseController(InventoryService service) : ControllerBase
{
    [HttpGet("inventory")]
    [Authorize(Roles = "WarehouseManager,WarehouseStaff")]
    public async Task<IActionResult> Inventory([FromQuery] int? warehouseId, [FromQuery] string? sku) => Ok(await service.GetInventoryAsync(warehouseId, sku));

    [HttpGet("transactions")]
    [Authorize(Roles = "WarehouseManager,WarehouseStaff")]
    public async Task<IActionResult> Transactions([FromQuery] int? warehouseId, [FromQuery] int? productId) => Ok(await service.GetTransactionsAsync(warehouseId, productId));

    [HttpPut("inventory/min-stock")]
    [Authorize(Roles = "WarehouseManager")]
    public async Task<IActionResult> SetMinStock(MinStockRequest request)
    {
        try { await service.SetMinStockAsync(request.WarehouseId, request.ProductId, request.MinStock); return NoContent(); }
        catch (InvalidOperationException exception) { return BadRequest(new { message = exception.Message }); }
    }

    [HttpGet("pending/receipts")]
    [Authorize(Roles = "WarehouseManager")]
    public async Task<IActionResult> PendingReceipts() => Ok(await service.GetPendingReceiptsAsync());

    [HttpGet("pending/issues")]
    [Authorize(Roles = "WarehouseManager")]
    public async Task<IActionResult> PendingIssues() => Ok(await service.GetPendingIssuesAsync());

    [HttpPost("receipts")]
    [Authorize(Roles = "WarehouseManager,WarehouseStaff")]
    public async Task<IActionResult> CreateReceipt(MovementRequest request)
    {
        try { return Ok(await service.CreateReceiptAsync(request.WarehouseId, CurrentUserId, request.Lines)); }
        catch (InvalidOperationException exception) { return BadRequest(new { message = exception.Message }); }
    }

    [HttpPost("receipts/{id:int}/approve")]
    [Authorize(Roles = "WarehouseManager")]
    public async Task<IActionResult> ApproveReceipt(int id)
    {
        try { await service.ApproveReceiptAsync(id, CurrentUserId); return NoContent(); }
        catch (InvalidOperationException exception) { return BadRequest(new { message = exception.Message }); }
    }

    [HttpPost("receipts/{id:int}/reject")]
    [Authorize(Roles = "WarehouseManager")]
    public async Task<IActionResult> RejectReceipt(int id, DecisionRequest request)
    {
        try { await service.RejectReceiptAsync(id, CurrentUserId, request.Reason); return NoContent(); }
        catch (InvalidOperationException exception) { return BadRequest(new { message = exception.Message }); }
    }

    [HttpPost("issues")]
    [Authorize(Roles = "WarehouseManager,WarehouseStaff")]
    public async Task<IActionResult> CreateIssue(MovementRequest request)
    {
        try { return Ok(await service.CreateIssueAsync(request.WarehouseId, CurrentUserId, request.Lines)); }
        catch (InvalidOperationException exception) { return BadRequest(new { message = exception.Message }); }
    }

    [HttpPost("issues/{id:int}/approve")]
    [Authorize(Roles = "WarehouseManager")]
    public async Task<IActionResult> ApproveIssue(int id)
    {
        try { await service.ApproveIssueAsync(id, CurrentUserId); return NoContent(); }
        catch (InvalidOperationException exception) { return BadRequest(new { message = exception.Message }); }
    }

    [HttpPost("issues/{id:int}/reject")]
    [Authorize(Roles = "WarehouseManager")]
    public async Task<IActionResult> RejectIssue(int id, DecisionRequest request)
    {
        try { await service.RejectIssueAsync(id, CurrentUserId, request.Reason); return NoContent(); }
        catch (InvalidOperationException exception) { return BadRequest(new { message = exception.Message }); }
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

public sealed record MovementRequest(int WarehouseId, IReadOnlyCollection<MovementLine> Lines);
public sealed record DecisionRequest(string Reason);
public sealed record MinStockRequest(int WarehouseId, int ProductId, int MinStock);
