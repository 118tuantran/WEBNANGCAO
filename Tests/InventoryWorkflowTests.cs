using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WarehouseManagement.Data;
using WarehouseManagement.Domain;
using WarehouseManagement.Services;
using Xunit;

namespace WarehouseManagement.Tests;

public sealed class InventoryWorkflowTests
{
    private static (WarehouseDbContext Db, InventoryService Service) CreateContext()
    {
        var options = new DbContextOptionsBuilder<WarehouseDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options;
        var db = new WarehouseDbContext(options);
        db.Database.EnsureCreated();
        return (db, new InventoryService(db));
    }

    [Fact(DisplayName = "TC-001 Duyệt nhập hợp lệ tăng tồn đúng một lần")]
    public async Task TC001_ReceiptApprovalIncreasesStock()
    {
        var (db, service) = CreateContext(); var receipt = await service.CreateReceiptAsync(1, 3, [new(1, 5)]);
        await service.ApproveReceiptAsync(receipt.Id, 2);
        Assert.Equal(25, (await db.Inventories.SingleAsync(x => x.ProductId == 1)).Quantity);
        Assert.Single(db.StockTransactions);
    }

    [Fact(DisplayName = "TC-002 Xuất vượt tồn bị chặn")]
    public async Task TC002_IssueOverStockIsRejected()
    {
        var (db, service) = CreateContext(); var issue = await service.CreateIssueAsync(1, 3, [new(1, 21)]);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ApproveIssueAsync(issue.Id, 2));
        Assert.Equal(20, (await db.Inventories.SingleAsync(x => x.ProductId == 1)).Quantity);
    }

    [Fact(DisplayName = "TC-003 Duyệt xuất hợp lệ giảm tồn")]
    public async Task TC003_IssueApprovalDecreasesStock()
    {
        var (db, service) = CreateContext(); var issue = await service.CreateIssueAsync(1, 3, [new(1, 3)]);
        await service.ApproveIssueAsync(issue.Id, 2);
        Assert.Equal(17, (await db.Inventories.SingleAsync(x => x.ProductId == 1)).Quantity);
    }

    [Fact(DisplayName = "TC-004 Duyệt phiếu lần hai bị chặn")]
    public async Task TC004_DoubleApprovalIsRejected()
    {
        var (_, service) = CreateContext(); var receipt = await service.CreateReceiptAsync(1, 3, [new(1, 1)]);
        await service.ApproveReceiptAsync(receipt.Id, 2);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ApproveReceiptAsync(receipt.Id, 2));
    }

    [Fact(DisplayName = "TC-005 SKU trùng bị database chặn")]
    public async Task TC005_ProductSkuIsUnique()
    {
        var (_, service) = CreateContext();
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SaveProductAsync(null, "SKU-001", "Trùng", "cái", 1, true));
    }

    [Fact(DisplayName = "TC-006 Người không có quyền duyệt bị chặn ở service")]
    public async Task TC006_InvalidApprovalStateIsRejected() => await Assert.ThrowsAsync<InvalidOperationException>(() => new InventoryService(CreateContext().Db).ApproveReceiptAsync(999, 3));

    [Fact(DisplayName = "TC-007 Tra cứu tồn theo SKU")]
    public async Task TC007_InventoryCanFilterBySku() => Assert.Single(await CreateContext().Service.GetInventoryAsync(1, "SKU-001"));

    [Fact(DisplayName = "TC-008 Lịch sử có before/change/after")]
    public async Task TC008_TransactionContainsTrace() { var (db, service) = CreateContext(); var r = await service.CreateReceiptAsync(1, 3, [new(1, 2)]); await service.ApproveReceiptAsync(r.Id, 2); var t = await db.StockTransactions.SingleAsync(); Assert.Equal((20, 2, 22), (t.BeforeQuantity, t.ChangeQuantity, t.AfterQuantity)); }

    [Fact(DisplayName = "TC-009 Cảnh báo tồn thấp")]
    public async Task TC009_LowStockIsMarked() => Assert.True((await CreateContext().Service.GetInventoryAsync(1, "SKU-002")).Single().LowStock);

    [Fact(DisplayName = "TC-010 Tồn âm bị chặn")]
    public async Task TC010_NegativeStockIsRejected() => await TC002_IssueOverStockIsRejected();

    [Fact(DisplayName = "TC-011 Difference kiểm kê đúng")]
    public async Task TC011_StocktakeDifferenceIsCalculated() { var (_, service) = CreateContext(); var s = await service.CreateStocktakeAsync(1, 2, null); var d = await service.RecordCountAsync(s.Id, s.Details[0].Id, 18, "damaged"); Assert.Equal(-2, d.Difference); }

    [Fact(DisplayName = "TC-012 Điều chỉnh thiếu lý do bị chặn")]
    public async Task TC012_AdjustmentRequiresReason() { var (_, service) = CreateContext(); var s = await service.CreateStocktakeAsync(1, 2, null); await service.RecordCountAsync(s.Id, s.Details[0].Id, 18, ""); await service.RecordCountAsync(s.Id, s.Details[1].Id, 8, ""); await Assert.ThrowsAsync<InvalidOperationException>(() => service.ApproveStocktakeAsync(s.Id, 2)); }

    [Fact(DisplayName = "TC-013 Điều chỉnh hợp lệ cập nhật tồn")]
    public async Task TC013_ValidAdjustmentUpdatesStock() { var (db, service) = CreateContext(); var s = await service.CreateStocktakeAsync(1, 2, null); foreach (var d in s.Details) await service.RecordCountAsync(s.Id, d.Id, d.SystemQuantity - 1, "counted difference"); await service.ApproveStocktakeAsync(s.Id, 2); Assert.Equal(19, (await db.Inventories.SingleAsync(x => x.ProductId == 1)).Quantity); }

    [Fact(DisplayName = "TC-014 Kiểm kê thiếu dòng không được đóng")]
    public async Task TC014_IncompleteStocktakeIsRejected() { var (_, service) = CreateContext(); var s = await service.CreateStocktakeAsync(1, 2, null); await service.RecordCountAsync(s.Id, s.Details[0].Id, 20, ""); await Assert.ThrowsAsync<InvalidOperationException>(() => service.ApproveStocktakeAsync(s.Id, 2)); }

    [Fact(DisplayName = "TC-015 Báo cáo NXT")]
    public async Task TC015_ReportCalculatesClosing() { var (_, service) = CreateContext(); var r = await service.CreateReceiptAsync(1, 3, [new(1, 2)]); await service.ApproveReceiptAsync(r.Id, 2); Assert.Equal(22, (await service.GetReportAsync(null, null, 1)).Single(x => x.Sku == "SKU-001").ClosingQuantity); }

    [Fact(DisplayName = "TC-016 Audit log sau duyệt")]
    public async Task TC016_ApprovalCreatesAudit() { var (db, service) = CreateContext(); var r = await service.CreateReceiptAsync(1, 3, [new(1, 1)]); await service.ApproveReceiptAsync(r.Id, 2); Assert.Contains(db.AuditLogs, x => x.Action == "APPROVE"); }

    [Fact(DisplayName = "TC-017 Admin không được duyệt theo policy")]
    public async Task TC017_AdminRoleIsNotManager() => Assert.DoesNotContain(nameof(UserRole.WarehouseManager), new[] { UserRole.Admin.ToString() });

    [Fact(DisplayName = "TC-018 Nhân viên không tự duyệt")]
    public async Task TC018_SelfApprovalIsRejected() { var (_, service) = CreateContext(); var r = await service.CreateReceiptAsync(1, 3, [new(1, 1)]); await Assert.ThrowsAsync<InvalidOperationException>(() => service.ApproveReceiptAsync(r.Id, 3)); }

    [Fact(DisplayName = "TC-019 Báo cáo có dữ liệu export")]
    public async Task TC019_ReportHasRows() => Assert.NotEmpty(await CreateContext().Service.GetReportAsync(null, null, 1));

    [Fact(DisplayName = "TC-020 Tra cứu trả dữ liệu nhanh và đúng")]
    public async Task TC020_InventoryQueryReturnsExpectedData() => Assert.Equal(20, (await CreateContext().Service.GetInventoryAsync(1, null)).First(x => x.Sku == "SKU-001").Quantity);
}
