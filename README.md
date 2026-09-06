# WEBNANGCAO - Hệ thống Quản lý Kho Hàng

Ứng dụng web bằng ASP.NET Core Web API, EF Core SQLite và giao diện HTML/CSS/JavaScript thuần, xây dựng theo tài liệu phân tích nghiệp vụ đi kèm.

## Tài liệu cho nhóm

- [CONTRIBUTING.md](CONTRIBUTING.md): quy ước branch, commit, Pull Request và phân công.
- [PROJECT-STRUCTURE.md](PROJECT-STRUCTURE.md): vai trò từng thư mục/file và luồng xử lý.
- `New Text Document.txt`: tài liệu phân tích, use case, requirements và thiết kế nguồn.

## Đưa lên GitHub lần đầu

Chạy trong terminal có cài Git:

```powershell
git init
git add .
git commit -m "chore: khoi tao he thong quan ly kho"
git branch -M main
git remote add origin <URL_REPOSITORY>
git push -u origin main
```

Sau khi tạo repository, thêm hai thành viên trong GitHub tại `Settings > Collaborators`.

## Chạy project

```powershell
dotnet run --urls http://localhost:5077
```

Database `warehouse.db` được tự tạo khi ứng dụng khởi động, kèm dữ liệu mẫu.

## Tài khoản demo

| Username | Password | Role |
|---|---|---|
| `admin` | `Admin@123` | Admin |
| `manager` | `Manager@123` | WarehouseManager |
| `staff` | `Staff@123` | WarehouseStaff |

API dùng cookie session sau khi gọi đăng nhập:

```http
POST /api/auth/login
Content-Type: application/json

{"username":"manager","password":"Manager@123"}
```

## API chính

- `GET /api/warehouse/inventory?warehouseId=1&sku=SKU-001`: tra cứu tồn và cảnh báo tồn thấp.
- `GET /api/warehouse/transactions?warehouseId=1`: xem lịch sử biến động.
- `POST /api/warehouse/receipts`: Nhân viên/Quản lý tạo phiếu nhập ở trạng thái `Pending`.
- `POST /api/warehouse/receipts/{id}/approve`: chỉ Quản lý duyệt, tồn tăng và ghi transaction.
- `POST /api/warehouse/issues`: Nhân viên/Quản lý tạo phiếu xuất ở trạng thái `Pending`.
- `POST /api/warehouse/issues/{id}/approve`: chỉ Quản lý duyệt, chặn tồn âm và ghi transaction.
- `POST /api/stocktakes`: Quản lý mở đợt kiểm kê.
- `PUT /api/stocktakes/{stocktakeId}/details/{detailId}`: Nhân viên nhập số thực tế.
- `POST /api/stocktakes/{id}/approve`: Quản lý duyệt điều chỉnh có lý do.
- `GET /api/reports/in-out-stock?warehouseId=1`: báo cáo nhập - xuất - tồn.
- `GET /api/admin/audit`: Admin xem audit log.
- `GET /api/reports/export/excel?warehouseId=1`: tải báo cáo Excel.
- `GET /api/reports/export/pdf?warehouseId=1`: tải báo cáo PDF.
- `POST /api/warehouse/receipts/{id}/reject`: từ chối phiếu nhập kèm lý do.
- `POST /api/warehouse/issues/{id}/reject`: từ chối phiếu xuất kèm lý do.
- `PUT /api/warehouse/inventory/min-stock`: cập nhật tồn tối thiểu.

Body tạo phiếu:

```json
{
  "warehouseId": 1,
  "lines": [{ "productId": 1, "quantity": 5 }]
}
```

## Quy tắc đã triển khai

- SKU và kho có mã duy nhất; dữ liệu mẫu có 2 SKU trong kho chính.
- Chỉ phiếu `Pending` mới được duyệt và phiếu đã duyệt không thể duyệt lần hai.
- Người tạo không được tự duyệt phiếu.
- Xuất vượt tồn bị từ chối; cập nhật tồn và `StockTransaction` trong cùng DB transaction.
- Tồn thấp khi `quantity <= min_stock`.
- Cookie authentication và RBAC ở API bằng 3 role theo tài liệu.
- Dữ liệu `StockTransaction` và `AuditLog` lưu user, thời gian, nguồn tham chiếu.

## Kiểm tra

```powershell
dotnet build
dotnet test .\Tests\WarehouseManagement.Tests.csproj
```

Lưu ý: restore hiện cảnh báo advisory từ dependency `SQLitePCLRaw.lib.e_sqlite3` do phiên bản native mà EF Core SQLite kéo theo.

## Cấu trúc file

| File/thư mục | Vai trò |
|---|---|
| `Program.cs` | Cấu hình DI, EF Core, cookie authentication, static files và middleware. |
| `Domain/Models.cs` | Entity, enum trạng thái và quy tắc dữ liệu cốt lõi của kho. |
| `Data/WarehouseDbContext.cs` | DbSet, unique index, quan hệ và dữ liệu mẫu SQLite. |
| `Services/InventoryService.cs` | Nghiệp vụ tạo/duyệt nhập xuất, tồn, kiểm kê, điều chỉnh và báo cáo. |
| `Controllers/AuthController.cs` | Đăng nhập/đăng xuất và tạo cookie session. |
| `Controllers/WarehouseController.cs` | API tồn kho, giao dịch, phiếu nhập và phiếu xuất. |
| `Controllers/StocktakeController.cs` | API mở kỳ, nhập đếm và duyệt điều chỉnh kiểm kê. |
| `Controllers/ReportController.cs` | API báo cáo nhập - xuất - tồn. |
| `Controllers/AdminController.cs` | API danh mục, kho và audit log cho Admin. |
| `Controllers/ExportController.cs` | Xuất báo cáo NXT thành Excel/PDF. |
| `wwwroot/index.html` | Giao diện web responsive: đăng nhập, tồn kho, phê duyệt, kiểm kê, báo cáo, audit. |
| `Tests/InventoryWorkflowTests.cs` | 20 test tự động đối chiếu TC-001 đến TC-020. |
| `Tests/WarehouseManagement.Tests.csproj` | Project test xUnit và EF Core InMemory. |
| `New Text Document.txt` | Tài liệu phân tích nghiệp vụ nguồn của đề tài; giữ lại để đối chiếu yêu cầu. |
| `warehouse.db` | SQLite database tạo tự động khi chạy, không cần commit vào source control. |

## Reset dữ liệu demo

Dừng server, xóa `warehouse.db`, rồi chạy lại `dotnet run`. Ứng dụng sẽ tạo lại schema và dữ liệu mẫu ban đầu.
