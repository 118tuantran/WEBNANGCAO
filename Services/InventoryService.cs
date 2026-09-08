using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Data;
using WarehouseManagement.Domain;

namespace WarehouseManagement.Services;

public sealed class InventoryService(WarehouseDbContext db)
{
    public async Task<Receipt> CreateReceiptAsync(int warehouseId, int userId, IReadOnlyCollection<MovementLine> lines)
    {
        ValidateLines(lines);
        await EnsureActiveWarehouseAsync(warehouseId);
        await EnsureProductsAsync(lines);
        var receipt = new Receipt { Number = $"GR-{DateTime.UtcNow:yyyyMMddHHmmssfff}", WarehouseId = warehouseId, CreatedBy = userId };
        receipt.Lines = lines.Select(x => new ReceiptLine { ProductId = x.ProductId, Quantity = x.Quantity }).ToList();
        db.Receipts.Add(receipt);
        await db.SaveChangesAsync();
        return receipt;
    }

    public async Task<Issue> CreateIssueAsync(int warehouseId, int userId, IReadOnlyCollection<MovementLine> lines)
    {
        ValidateLines(lines);
        await EnsureActiveWarehouseAsync(warehouseId);
        await EnsureProductsAsync(lines);
        var issue = new Issue { Number = $"GI-{DateTime.UtcNow:yyyyMMddHHmmssfff}", WarehouseId = warehouseId, CreatedBy = userId };
        issue.Lines = lines.Select(x => new IssueLine { ProductId = x.ProductId, Quantity = x.Quantity }).ToList();
        db.Issues.Add(issue);
        await db.SaveChangesAsync();
        return issue;
    }

    public async Task ApproveReceiptAsync(int id, int approverId)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        var receipt = await db.Receipts.Include(x => x.Lines).SingleOrDefaultAsync(x => x.Id == id)
            ?? throw new InvalidOperationException("Không tìm thấy phiếu nhập.");
        EnsurePending(receipt.Status);
        EnsureNotSelfApproval(receipt.CreatedBy, approverId);
        foreach (var line in receipt.Lines)
            await ApplyMovementAsync(receipt.WarehouseId, line.ProductId, line.Quantity, MovementType.Receipt, "GoodsReceipt", id, approverId);
        receipt.Status = DocumentStatus.Approved;
        receipt.ApprovedBy = approverId;
        receipt.ApprovedAt = DateTime.UtcNow;
        db.AuditLogs.Add(new AuditLog { UserId = approverId, Action = "APPROVE", EntityType = "GoodsReceipt", EntityId = id });
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task ApproveIssueAsync(int id, int approverId)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        var issue = await db.Issues.Include(x => x.Lines).SingleOrDefaultAsync(x => x.Id == id)
            ?? throw new InvalidOperationException("Không tìm thấy phiếu xuất.");
        EnsurePending(issue.Status);
        EnsureNotSelfApproval(issue.CreatedBy, approverId);
        foreach (var line in issue.Lines)
            await ApplyMovementAsync(issue.WarehouseId, line.ProductId, -line.Quantity, MovementType.Issue, "GoodsIssue", id, approverId);
        issue.Status = DocumentStatus.Approved;
        issue.ApprovedBy = approverId;
        issue.ApprovedAt = DateTime.UtcNow;
        db.AuditLogs.Add(new AuditLog { UserId = approverId, Action = "APPROVE", EntityType = "GoodsIssue", EntityId = id });
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public Task<List<InventoryView>> GetInventoryAsync(int? warehouseId, string? sku)
    {
        var query = db.Inventories.AsNoTracking();
        if (warehouseId.HasValue) query = query.Where(x => x.WarehouseId == warehouseId.Value);
        if (!string.IsNullOrWhiteSpace(sku)) query = query.Where(x => x.Product!.Sku == sku);
        return query.OrderBy(x => x.Product!.Sku).Select(x => new InventoryView(x.WarehouseId, x.Product!.Sku, x.Product.Name, x.Quantity, x.MinStock, x.Quantity <= x.MinStock)).ToListAsync();
    }

    public Task<List<StockTransaction>> GetTransactionsAsync(int? warehouseId, int? productId) =>
        db.StockTransactions.AsNoTracking().Where(x => !warehouseId.HasValue || x.WarehouseId == warehouseId.Value).Where(x => !productId.HasValue || x.ProductId == productId.Value).OrderByDescending(x => x.CreatedAt).Take(200).ToListAsync();

    public Task<List<Product>> GetProductsAsync() => db.Products.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Sku).ToListAsync();
    public Task<List<Warehouse>> GetWarehousesAsync() => db.Warehouses.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Code).ToListAsync();
    public Task<List<Receipt>> GetPendingReceiptsAsync() => db.Receipts.AsNoTracking().Include(x => x.Lines).Where(x => x.Status == DocumentStatus.Pending).OrderByDescending(x => x.CreatedAt).ToListAsync();
    public Task<List<Issue>> GetPendingIssuesAsync() => db.Issues.AsNoTracking().Include(x => x.Lines).Where(x => x.Status == DocumentStatus.Pending).OrderByDescending(x => x.CreatedAt).ToListAsync();

    public async Task<Stocktake> CreateStocktakeAsync(int warehouseId, int userId, IReadOnlyCollection<int>? productIds)
    {
        await EnsureActiveWarehouseAsync(warehouseId);
        var ids = productIds is { Count: > 0 } ? productIds : await db.Inventories.Where(x => x.WarehouseId == warehouseId).Select(x => x.ProductId).ToListAsync();
        var inventories = await db.Inventories.Where(x => x.WarehouseId == warehouseId && ids.Contains(x.ProductId)).ToListAsync();
        if (inventories.Count == 0) throw new InvalidOperationException("Kho chưa có SKU để kiểm kê.");
        var stocktake = new Stocktake { Number = $"ST-{DateTime.UtcNow:yyyyMMddHHmmssfff}", WarehouseId = warehouseId, CreatedBy = userId };
        stocktake.Details = inventories.Select(x => new StocktakeDetail { ProductId = x.ProductId, SystemQuantity = x.Quantity }).ToList();
        db.Stocktakes.Add(stocktake);
        await db.SaveChangesAsync();
        return stocktake;
    }

    public async Task<StocktakeDetail> RecordCountAsync(int stocktakeId, int detailId, int actualQuantity, string? reason)
    {
        if (actualQuantity < 0) throw new InvalidOperationException("Số thực tế không được âm.");
        var detail = await db.StocktakeDetails.Include(x => x.Stocktake).SingleOrDefaultAsync(x => x.Id == detailId && x.StocktakeId == stocktakeId)
            ?? throw new InvalidOperationException("Không tìm thấy dòng kiểm kê.");
        if (detail.Stocktake!.Status == StocktakeStatus.Closed) throw new InvalidOperationException("Đợt kiểm kê không còn mở.");
        detail.ActualQuantity = actualQuantity;
        detail.Difference = actualQuantity - detail.SystemQuantity;
        detail.Reason = reason;
        detail.Stocktake.Status = StocktakeStatus.Review;
        await db.SaveChangesAsync();
        return detail;
    }

    public Task<Stocktake?> GetStocktakeAsync(int id) => db.Stocktakes.AsNoTracking().Include(x => x.Details).ThenInclude(x => x.Product).SingleOrDefaultAsync(x => x.Id == id);

    public async Task ApproveStocktakeAsync(int stocktakeId, int approverId)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        var stocktake = await db.Stocktakes.Include(x => x.Details).SingleOrDefaultAsync(x => x.Id == stocktakeId)
            ?? throw new InvalidOperationException("Không tìm thấy đợt kiểm kê.");
        if (stocktake.Status == StocktakeStatus.Closed) throw new InvalidOperationException("Đợt kiểm kê đã đóng.");
        if (stocktake.Details.Any(x => x.ActualQuantity is null)) throw new InvalidOperationException("Chưa nhập đủ số thực tế.");
        foreach (var detail in stocktake.Details)
        {
            if (detail.Difference == 0) { detail.IsAdjusted = true; continue; }
            if (string.IsNullOrWhiteSpace(detail.Reason)) throw new InvalidOperationException("Dòng chênh lệch bắt buộc có lý do.");
            await ApplyMovementAsync(stocktake.WarehouseId, detail.ProductId, detail.Difference!.Value, MovementType.Adjustment, "Stocktake", stocktakeId, approverId);
            detail.IsAdjusted = true;
        }
        stocktake.Status = StocktakeStatus.Closed;
        stocktake.ApprovedBy = approverId;
        db.AuditLogs.Add(new AuditLog { UserId = approverId, Action = "ADJUST_STOCK", EntityType = "Stocktake", EntityId = stocktakeId });
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task<List<ReportRow>> GetReportAsync(DateTime? from, DateTime? to, int? warehouseId)
    {
        var end = to?.Date.AddDays(1) ?? DateTime.UtcNow;
        var query = db.StockTransactions.AsNoTracking().Where(x => x.CreatedAt < end && (!warehouseId.HasValue || x.WarehouseId == warehouseId.Value));
        var inPeriod = query.Where(x => !from.HasValue || x.CreatedAt >= from.Value.Date);
        var movements = await inPeriod.GroupBy(x => x.ProductId).Select(x => new { ProductId = x.Key, Receipt = x.Where(y => y.Type == MovementType.Receipt).Sum(y => y.ChangeQuantity), Issue = -x.Where(y => y.Type == MovementType.Issue).Sum(y => y.ChangeQuantity), Adjustment = x.Where(y => y.Type == MovementType.Adjustment).Sum(y => y.ChangeQuantity) }).ToListAsync();
        var inventory = await db.Inventories.AsNoTracking()
            .Where(x => !warehouseId.HasValue || x.WarehouseId == warehouseId.Value)
            .Select(x => new { x.ProductId, x.Quantity, Sku = x.Product!.Sku, ProductName = x.Product.Name })
            .ToListAsync();
        return inventory.GroupJoin(movements, x => x.ProductId, x => x.ProductId, (item, changes) => { var c = changes.SingleOrDefault(); var receipt = c?.Receipt ?? 0; var issue = c?.Issue ?? 0; var adjustment = c?.Adjustment ?? 0; return new ReportRow(item.Sku, item.ProductName, item.Quantity - receipt + issue - adjustment, receipt, issue, adjustment, item.Quantity); }).OrderBy(x => x.Sku).ToList();
    }

    private async Task ApplyMovementAsync(int warehouseId, int productId, int change, MovementType type, string referenceType, int referenceId, int userId)
    {
        var inventory = await db.Inventories.SingleOrDefaultAsync(x => x.WarehouseId == warehouseId && x.ProductId == productId)
            ?? throw new InvalidOperationException("SKU chưa được cấu hình trong kho.");
        var after = inventory.Quantity + change;
        if (after < 0) throw new InvalidOperationException("Không đủ tồn khả dụng để duyệt phiếu xuất.");
        var before = inventory.Quantity;
        inventory.Quantity = after;
        db.StockTransactions.Add(new StockTransaction { WarehouseId = warehouseId, ProductId = productId, Type = type, BeforeQuantity = before, ChangeQuantity = change, AfterQuantity = after, ReferenceType = referenceType, ReferenceId = referenceId, CreatedBy = userId });
    }

    private async Task EnsureProductsAsync(IEnumerable<MovementLine> lines)
    {
        var ids = lines.Select(x => x.ProductId).ToArray();
        if (await db.Products.CountAsync(x => ids.Contains(x.Id) && x.IsActive) != ids.Length) throw new InvalidOperationException("SKU không tồn tại hoặc đã bị khóa.");
    }
    private async Task EnsureActiveWarehouseAsync(int id) { if (!await db.Warehouses.AnyAsync(x => x.Id == id && x.IsActive)) throw new InvalidOperationException("Kho không tồn tại hoặc đã bị khóa."); }
    private static void ValidateLines(IReadOnlyCollection<MovementLine> lines) { if (lines.Count == 0 || lines.Any(x => x.Quantity <= 0)) throw new InvalidOperationException("Danh sách SKU không được rỗng và số lượng phải lớn hơn 0."); }
    private static void EnsurePending(DocumentStatus status) { if (status != DocumentStatus.Pending) throw new InvalidOperationException("Chứng từ không ở trạng thái chờ duyệt."); }
    private static void EnsureNotSelfApproval(int creatorId, int approverId) { if (creatorId == approverId) throw new InvalidOperationException("Người tạo không được tự duyệt chứng từ."); }
}

public record MovementLine(int ProductId, int Quantity);
public record InventoryView(int WarehouseId, string Sku, string ProductName, int Quantity, int MinStock, bool LowStock);
public record ReportRow(string Sku, string ProductName, int OpeningQuantity, int ReceiptQuantity, int IssueQuantity, int AdjustmentQuantity, int ClosingQuantity);
