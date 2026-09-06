using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WarehouseManagement.Services;

namespace WarehouseManagement.Controllers;

[ApiController]
[Route("api/reports/export")]
[Authorize(Roles = "WarehouseManager")]
public sealed class ExportController(InventoryService service) : ControllerBase
{
    [HttpGet("excel")]
    public async Task<IActionResult> Excel([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int? warehouseId)
    {
        var rows = await service.GetReportAsync(from, to, warehouseId);
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("NXT");
        sheet.Cell(1, 1).Value = "Báo cáo nhập - xuất - tồn";
        sheet.Range(1, 1, 1, 7).Merge().Style.Font.Bold = true;
        var headers = new[] { "SKU", "Sản phẩm", "Tồn đầu", "Nhập", "Xuất", "Điều chỉnh", "Tồn cuối" };
        for (var column = 0; column < headers.Length; column++) sheet.Cell(3, column + 1).Value = headers[column];
        sheet.Range(3, 1, 3, headers.Length).Style.Font.Bold = true;
        for (var index = 0; index < rows.Count; index++)
        {
            var row = rows[index]; var line = index + 4;
            sheet.Cell(line, 1).Value = row.Sku; sheet.Cell(line, 2).Value = row.ProductName;
            sheet.Cell(line, 3).Value = row.OpeningQuantity; sheet.Cell(line, 4).Value = row.ReceiptQuantity;
            sheet.Cell(line, 5).Value = row.IssueQuantity; sheet.Cell(line, 6).Value = row.AdjustmentQuantity; sheet.Cell(line, 7).Value = row.ClosingQuantity;
        }
        sheet.Columns().AdjustToContents();
        using var stream = new MemoryStream(); workbook.SaveAs(stream);
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "bao-cao-nhap-xuat-ton.xlsx");
    }

    [HttpGet("pdf")]
    public async Task<IActionResult> Pdf([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int? warehouseId)
    {
        var rows = await service.GetReportAsync(from, to, warehouseId);
        var document = Document.Create(container => container.Page(page =>
        {
            page.Margin(30);
            page.Header().Text("BÁO CÁO NHẬP - XUẤT - TỒN").FontSize(18).Bold();
            page.Content().Table(table =>
            {
                table.ColumnsDefinition(columns => { for (var i = 0; i < 7; i++) columns.RelativeColumn(); });
                foreach (var header in new[] { "SKU", "Sản phẩm", "Tồn đầu", "Nhập", "Xuất", "Đ/c", "Tồn cuối" }) table.Cell().Element(HeaderCell).Text(header);
                foreach (var row in rows)
                {
                    table.Cell().Text(row.Sku); table.Cell().Text(row.ProductName); table.Cell().Text(row.OpeningQuantity.ToString());
                    table.Cell().Text(row.ReceiptQuantity.ToString()); table.Cell().Text(row.IssueQuantity.ToString());
                    table.Cell().Text(row.AdjustmentQuantity.ToString()); table.Cell().Text(row.ClosingQuantity.ToString());
                }
            });
            page.Footer().AlignCenter().Text($"Tạo lúc {DateTime.Now:dd/MM/yyyy HH:mm}");
        }));
        return File(document.GeneratePdf(), "application/pdf", "bao-cao-nhap-xuat-ton.pdf");
    }

    private static IContainer HeaderCell(IContainer container) => container.Background(Colors.Grey.Lighten2).Padding(5).DefaultTextStyle(x => x.Bold());
}
