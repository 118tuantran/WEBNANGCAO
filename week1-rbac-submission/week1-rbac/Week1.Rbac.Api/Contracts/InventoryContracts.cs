using System.ComponentModel.DataAnnotations;

namespace Week1.Rbac.Api.Contracts;

public sealed record UpdateInventoryRequest(
    [property: Required] Guid WarehouseId,
    [property: Required] Guid ProductId,
    [property: Range(0, int.MaxValue)] int Quantity);

public sealed record InventoryResponse(
    Guid WarehouseId,
    string WarehouseCode,
    string WarehouseName,
    Guid ProductId,
    string ProductCode,
    string ProductName,
    int Quantity,
    DateTimeOffset LastUpdated);
