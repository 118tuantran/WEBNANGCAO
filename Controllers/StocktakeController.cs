using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseManagement.Services;

namespace WarehouseManagement.Controllers;

[ApiController]
[Route("api/stocktakes")]
[Authorize(Roles = "WarehouseManager,WarehouseStaff")]
public sealed class StocktakeController(InventoryService service) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "WarehouseManager")]
    public async Task<IActionResult> Create(StocktakeRequest request)
    {
        try { return Ok(await service.CreateStocktakeAsync(request.WarehouseId, UserId, request.ProductIds)); }
        catch (InvalidOperationException e) { return BadRequest(new { message = e.Message }); }
    }

    [HttpPut("{stocktakeId:int}/details/{detailId:int}")]
    public async Task<IActionResult> Count(int stocktakeId, int detailId, CountRequest request)
    {
        try { return Ok(await service.RecordCountAsync(stocktakeId, detailId, request.ActualQuantity, request.Reason)); }
        catch (InvalidOperationException e) { return BadRequest(new { message = e.Message }); }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id) => (await service.GetStocktakeAsync(id)) is { } stocktake ? Ok(stocktake) : NotFound();

    [HttpPost("{id:int}/approve")]
    [Authorize(Roles = "WarehouseManager")]
    public async Task<IActionResult> Approve(int id)
    {
        try { await service.ApproveStocktakeAsync(id, UserId); return NoContent(); }
        catch (InvalidOperationException e) { return BadRequest(new { message = e.Message }); }
    }

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

public sealed record StocktakeRequest(int WarehouseId, IReadOnlyCollection<int>? ProductIds);
public sealed record CountRequest(int ActualQuantity, string? Reason);
