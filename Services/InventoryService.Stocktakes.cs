using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Domain;

namespace WarehouseManagement.Services;

// Kiểm kê theo mục 6.5: mở đợt (OPEN) → nhập số thực tế → đủ số liệu (REVIEW) → duyệt điều chỉnh có lý do (CLOSED).
public sealed partial class InventoryService
{
    public async Task<Stocktake> CreateStocktakeAsync(int warehouseId, int userId, IReadOnlyCollection<int>? productIds)
    {
        await EnsureActiveWarehouseAsync(warehouseId);
        if (await db.Stocktakes.AnyAsync(x => x.WarehouseId == warehouseId && x.Status != StocktakeStatus.Closed))
            throw new InvalidOperationException("Kho đang có đợt kiểm kê chưa đóng; hãy duyệt và đóng đợt đó trước khi mở đợt mới.");

        var selectedProductIds = productIds is { Count: > 0 }
            ? productIds.Distinct().ToList()
            : await db.Inventories.AsNoTracking().Where(x => x.WarehouseId == warehouseId).Select(x => x.ProductId).ToListAsync();

        var inventories = await db.Inventories.AsNoTracking()
            .Where(x => x.WarehouseId == warehouseId && selectedProductIds.Contains(x.ProductId))
            .Select(x => new { x.ProductId, x.Quantity })
            .ToListAsync();

        if (inventories.Count == 0) throw new InvalidOperationException("Kho chưa có SKU để kiểm kê.");
        if (productIds is { Count: > 0 } && inventories.Count != selectedProductIds.Count)
            throw new InvalidOperationException("Có SKU không thuộc kho đã chọn.");

        var stocktake = new Stocktake { Number = NewNumber("ST"), WarehouseId = warehouseId, CreatedBy = userId };
        stocktake.Details = inventories.Select(x => new StocktakeDetail { ProductId = x.ProductId, SystemQuantity = x.Quantity }).ToList();
        await AddWithAuditAsync(stocktake, () => stocktake.Id, userId, StocktakeEntity, $"{stocktake.Number} · {inventories.Count} SKU");
        return stocktake;
    }

    // Nhân viên chỉ nhập số thực tế (US-012); lý do điều chỉnh do Quản lý nhập khi xem chênh lệch (mục 6.5 bước 5).
    public async Task<StocktakeDetail> RecordCountAsync(int stocktakeId, int detailId, int actualQuantity, string? reason, int userId = 0, bool canSetReason = true)
    {
        if (actualQuantity < 0) throw new InvalidOperationException("Số thực tế không được âm.");
        if (!canSetReason && !string.IsNullOrWhiteSpace(reason)) throw new ForbiddenOperationException("Nhân viên kho chỉ nhập số thực tế; lý do điều chỉnh do Quản lý kho nhập khi xem chênh lệch.");

        var stocktake = await db.Stocktakes.Include(x => x.Details).ThenInclude(x => x.Product).SingleOrDefaultAsync(x => x.Id == stocktakeId)
            ?? throw new InvalidOperationException("Không tìm thấy đợt kiểm kê.");
        if (stocktake.Status == StocktakeStatus.Closed) throw new InvalidOperationException("Đợt kiểm kê đã đóng, không thể sửa số liệu.");

        var detail = stocktake.Details.SingleOrDefault(x => x.Id == detailId) ?? throw new InvalidOperationException("Không tìm thấy dòng kiểm kê.");
        detail.ActualQuantity = actualQuantity;
        detail.Difference = actualQuantity - detail.SystemQuantity;
        if (canSetReason) detail.Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();

        stocktake.Status = ResolveStocktakeStatus(stocktake.Details);
        Audit(userId, "COUNT", StocktakeEntity, stocktakeId, $"{detail.Product?.Sku}: thực tế {actualQuantity}, hệ thống {detail.SystemQuantity}, chênh lệch {detail.Difference:+#;-#;0}");
        await db.SaveChangesAsync();
        return detail;
    }

    public async Task<List<StocktakeSummary>> GetStocktakesAsync(StocktakeStatus? status)
    {
        var rows = await db.Stocktakes.AsNoTracking().Where(x => !status.HasValue || x.Status == status.Value).OrderByDescending(x => x.CreatedAt).Take(200)
            .Select(x => new { x.Id, x.Number, x.WarehouseId, x.Status, x.CreatedBy, x.CreatedAt, x.ApprovedBy, LineCount = x.Details.Count,
                Counted = x.Details.Count(d => d.ActualQuantity != null), Differences = x.Details.Count(d => d.Difference != null && d.Difference != 0) })
            .ToListAsync();
        var warehouses = await WarehouseCodesAsync();
        var users = await UserNamesAsync();
        return rows.Select(x => new StocktakeSummary(x.Id, x.Number, x.WarehouseId, warehouses.GetValueOrDefault(x.WarehouseId, "?"), x.Status, x.CreatedBy, UserName(users, x.CreatedBy), x.CreatedAt,
            x.ApprovedBy is { } approver ? UserName(users, approver) : null, x.LineCount, x.Counted, x.Differences)).ToList();
    }

    public async Task<StocktakeView?> GetStocktakeAsync(int id)
    {
        var stocktake = await db.Stocktakes.AsNoTracking().Include(x => x.Details).ThenInclude(x => x.Product).SingleOrDefaultAsync(x => x.Id == id);
        if (stocktake is null) return null;
        var users = await UserNamesAsync();
        var warehouses = await WarehouseCodesAsync();
        return new StocktakeView(stocktake.Id, stocktake.Number, stocktake.WarehouseId, warehouses.GetValueOrDefault(stocktake.WarehouseId, "?"), stocktake.Status, stocktake.CreatedBy, UserName(users, stocktake.CreatedBy),
            stocktake.CreatedAt, stocktake.ApprovedBy is { } approver ? UserName(users, approver) : null,
            stocktake.Details.OrderBy(x => x.Product!.Sku).Select(x => new StocktakeLineView(x.Id, x.ProductId, x.Product!.Sku, x.Product.Name, x.Product.Unit, x.SystemQuantity, x.ActualQuantity, x.Difference, x.Reason, x.IsAdjusted)).ToList());
    }

    public async Task ApproveStocktakeAsync(int stocktakeId, int approverId)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        var stocktake = await db.Stocktakes.Include(x => x.Details).SingleOrDefaultAsync(x => x.Id == stocktakeId)
            ?? throw new InvalidOperationException("Không tìm thấy đợt kiểm kê.");
        if (stocktake.Status == StocktakeStatus.Closed) throw new InvalidOperationException("Đợt kiểm kê đã đóng.");
        if (stocktake.Details.Count == 0) throw new InvalidOperationException("Đợt kiểm kê không có dòng SKU nào để duyệt.");
        if (stocktake.Details.Any(x => x.ActualQuantity is null)) throw new InvalidOperationException("Chưa nhập đủ số thực tế.");

        var adjusted = 0;
        foreach (var detail in stocktake.Details)
        {
            if (detail.Difference == 0)
            {
                detail.IsAdjusted = true;
                continue;
            }

            if (string.IsNullOrWhiteSpace(detail.Reason)) throw new InvalidOperationException("Dòng chênh lệch bắt buộc có lý do.");
            await ApplyMovementAsync(stocktake.WarehouseId, detail.ProductId, detail.Difference!.Value, MovementType.Adjustment, StocktakeEntity, stocktakeId, approverId);
            detail.IsAdjusted = true;
            adjusted++;
        }

        stocktake.Status = StocktakeStatus.Closed;
        stocktake.ApprovedBy = approverId;
        Audit(approverId, "ADJUST_STOCK", StocktakeEntity, stocktakeId, $"{stocktake.Number} · điều chỉnh {adjusted} SKU");
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private static StocktakeStatus ResolveStocktakeStatus(IEnumerable<StocktakeDetail> details)
        => details.All(x => x.ActualQuantity.HasValue) ? StocktakeStatus.Review : StocktakeStatus.Open;
}

public record StocktakeSummary(int Id, string Number, int WarehouseId, string WarehouseCode, StocktakeStatus Status, int CreatedBy, string CreatedByName, DateTime CreatedAt, string? ApprovedByName, int LineCount, int CountedCount, int DifferenceCount);
public record StocktakeLineView(int DetailId, int ProductId, string Sku, string ProductName, string Unit, int SystemQuantity, int? ActualQuantity, int? Difference, string? Reason, bool IsAdjusted);
public record StocktakeView(int Id, string Number, int WarehouseId, string WarehouseCode, StocktakeStatus Status, int CreatedBy, string CreatedByName, DateTime CreatedAt, string? ApprovedByName, List<StocktakeLineView> Lines);
