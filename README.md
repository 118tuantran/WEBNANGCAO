# WEBNANGCAO - Hệ thống Quản lý Kho Hàng

Ứng dụng web bằng ASP.NET Core Web API, EF Core MySQL và giao diện HTML/CSS/JavaScript thuần, xây dựng theo tài liệu phân tích nghiệp vụ đi kèm.

## Tài liệu cho nhóm

- [CONTRIBUTING.md](CONTRIBUTING.md): quy ước branch, commit, Pull Request và phân công.
- [PROJECT-STRUCTURE.md](PROJECT-STRUCTURE.md): vai trò từng thư mục/file và luồng xử lý.
- `file phân tích yêu cầu hệ thống.txt`: tài liệu phân tích, use case, requirements và thiết kế nguồn.

## Repository GitHub

Repository chính của project:

https://github.com/118tuantran/WEBNANGCAO

Các lệnh thường dùng:

```powershell
git pull origin main
git status
git add .
git commit -m "mo ta thay doi"
git push origin main
```

## Chạy project

```powershell
dotnet run --urls http://localhost:5165
```

Database MySQL `webnangcao` được cập nhật bằng EF Core migrations khi ứng dụng khởi động. Với XAMPP, bật MySQL trên port `3306` trước khi chạy.

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

## Cấu trúc file

| File/thư mục | Vai trò |
|---|---|
| `Program.cs` | Cấu hình DI, EF Core, cookie authentication, static files và middleware. |
| `Domain/Models.cs` | Entity, enum trạng thái và quy tắc dữ liệu cốt lõi của kho. |
| `Data/WarehouseDbContext.cs` | DbSet, unique index, quan hệ và dữ liệu mẫu MySQL. |
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
| `file phân tích yêu cầu hệ thống.txt` | Tài liệu phân tích nghiệp vụ nguồn của đề tài; giữ lại để đối chiếu yêu cầu. |
| `Data/Migrations/` | Lịch sử thay đổi schema bằng EF Core migrations. |
| `Database/backup-mysql.ps1` | Sao lưu database MySQL bằng mysqldump. |
| `Database/restore-mysql.ps1` | Khôi phục database từ file SQL. |
| `Tests/api-smoke.ps1` | Smoke test login, RBAC, health, Swagger và latency API. |
| `Dockerfile` / `docker-compose.yml` | Cấu hình chạy app cùng MySQL bằng Docker. |

## Đối chiếu với file phân tích yêu cầu

File `file phân tích yêu cầu hệ thống.txt` là tài liệu yêu cầu nghiệp vụ gốc của dự án. Dựa trên hiện trạng thực tế của project, mình đánh giá như sau:

### 1. Mức độ hoàn thành chức năng theo file yêu cầu

| Nhóm yêu cầu | Trạng thái | Mức độ | Ghi chú |
|---|---|---:|---|
| Đăng nhập / đăng xuất / RBAC 3 role | Hoàn thành | 90% | Có Admin, WarehouseManager, WarehouseStaff; login cookie + authorize hoạt động rõ ràng. |
| Quản lý SKU / danh mục / kho | Hoàn thành | 90% | Manager có quyền CRUD master data; dữ liệu seed sẵn và có unique constraint. |
| Nhập kho và duyệt phiếu nhập | Hoàn thành | 90% | Tạo phiếu, duyệt, tăng tồn, log transaction, audit log. |
| Xuất kho và duyệt phiếu xuất | Hoàn thành | 90% | Có kiểm tra tồn, chặn tồn âm, transaction trong cùng DB transaction. |
| Tra cứu tồn và lịch sử biến động | Hoàn thành | 90% | Có inventory + stock transactions; dữ liệu minh bạch. |
| Kiểm kê và điều chỉnh tồn | Hoàn thành | 85% | Tạo stocktake, ghi actual qty, tính difference, duyệt điều chỉnh. |
| Cảnh báo tồn thấp | Hoàn thành | 85% | Dựa trên min_stock và low stock flag. |
| Báo cáo NXT | Hoàn thành | 85% | Có báo cáo tổng hợp, export Excel/PDF. |
| Audit log | Hoàn thành | 85% | Có ghi log khi login, approve, reject, access errors. |
| Test nghiệp vụ | Hoàn thành | 100% | Có 20 test xUnit với các case chính. |

### 2. Tỷ lệ hoàn thành ước tính

- Về chức năng nghiệp vụ chính theo file phân tích: khoảng 85% - 90%
- Về production-ready / hoàn thiện hệ thống thực tế: khoảng 60% - 70%

Nói ngắn gọn: project đã đáp ứng tốt các yêu cầu cốt lõi của môn học / demo nghiệp vụ kho; phần production vẫn còn performance/integration test, triển khai production và kiểm thử UI.

### 3. Yêu cầu đã hoàn thành chính theo file phân tích

| ID yêu cầu | Mô tả | Kết quả |
|---|---|---|
| UC-01 | Đăng nhập / Đăng xuất | Hoàn thành |
| UC-02 | Quản lý SKU | Hoàn thành |
| UC-03 | Quản lý kho | Hoàn thành |
| UC-04 | Thiết lập ngưỡng tồn | Hoàn thành |
| UC-05 | Tạo phiếu nhập | Hoàn thành |
| UC-06 | Duyệt phiếu nhập | Hoàn thành |
| UC-07 | Tạo phiếu xuất | Hoàn thành |
| UC-08 | Duyệt phiếu xuất và chặn vượt tồn | Hoàn thành |
| UC-09 | Tra cứu tồn | Hoàn thành |
| UC-10 | Xem lịch sử biến động | Hoàn thành |
| UC-11 | Tạo đợt kiểm kê | Hoàn thành |
| UC-12 | Ghi nhận kiểm kê | Hoàn thành |
| UC-13 | Duyệt điều chỉnh tồn | Hoàn thành |
| UC-14 | Cảnh báo tồn thấp | Hoàn thành |
| UC-15 | Báo cáo nhập - xuất - tồn | Hoàn thành |
| US-001 | Admin quản lý user/role | Hoàn thành |
| US-002 | Đăng nhập theo role | Hoàn thành |
| US-005..US-015 | Đa số user story nghiệp vụ | Hoàn thành |
| FR-001..FR-019 | Phần lớn functional requirements | Đã có gần hết |
| NFR-01..NFR-05 | Hiệu năng / nhất quán / quyền / truy vết / usability | Đạt phần lớn |

### 4. Những phần còn thiếu hoặc chưa hoàn thiện so với file phân tích

| Vấn đề | Tình trạng | Mức độ cần làm |
|---|---|---|
| Role/Supplier/unit cost | Đã bổ sung model, bảng, migration và API Supplier | Hoàn thành |
| Migration chuẩn | Dùng `MigrateAsync()` và có `Data/Migrations/` | Hoàn thành |
| Swagger / OpenAPI | Có tại `/swagger` | Hoàn thành |
| Validation DTO | Có DataAnnotations và validation nghiệp vụ | Hoàn thành cơ bản |
| Global exception handling | Có middleware trả JSON lỗi tập trung | Hoàn thành cơ bản |
| Health check | Có `/health` kiểm tra EF/MySQL | Hoàn thành |
| CI/CD | Có GitHub Actions build/test | Hoàn thành CI |
| Backup / restore | Có script PowerShell và đã test backup thực tế | Hoàn thành cơ bản |
| Performance test | Smoke test 20 lần, trung bình khoảng 18.52 ms; chưa phải load test lớn | Hoàn thành mức đồ án |
| Integration/RBAC test | `Tests/api-smoke.ps1` kiểm thử API thật với cookie và 3 role | Hoàn thành mức smoke |
| Deploy production | Có Dockerfile và Docker Compose; máy hiện không có Docker daemon để chạy thử | Cấu hình hoàn thành, runtime chưa xác minh |
| Responsive UI/UX | Playwright đã kiểm tra desktop/tablet/mobile, không tràn ngang; đã tách màn hình theo role | Hoàn thành mức đồ án |

### 5. Môi trường và ứng dụng mà project này phải chạy trên

Theo file phân tích và cấu trúc hiện tại, project này cần chạy trên các môi trường sau:

- Hệ điều hành: Windows 10/11 (do workspace và dự án .NET chạy trên Windows)
- Runtime / Framework: .NET 10 SDK + ASP.NET Core Web App
- Database: MySQL `webnangcao` trên XAMPP
- Browser: Chrome, Edge, Firefox mới nhất
- Thiết bị phục vụ: desktop / laptop, có thể dùng tablet với giao diện responsive
- Mục tiêu người dùng: Admin, Quản lý kho, Nhân viên kho

### 6. Kiểm tra thực tế project hiện tại

Tôi đã chạy kiểm tra bằng lệnh sau:

`dotnet test .\Tests\WarehouseManagement.Tests.csproj --nologo`

Kết quả thực tế:

- Tổng test: 20
- Failed: 0
- Passed: 20
- Status: dự án nghiệp vụ cốt lõi đang hoạt động tốt

### 7. Kết luận tổng thể

Project này đã hiện thực đúng hướng theo file phân tích ở mức nghiệp vụ chính:

- RBAC 3 role
- Quản lý kho, SKU, warehouse, min_stock
- Phiếu nhập / xuất / duyệt
- Tồn kho, stock transaction, audit log
- Kiểm kê và điều chỉnh tồn
- Báo cáo NXT và export Excel/PDF
- 20 test nghiệp vụ kiểm tra chính

Các phần còn lại chủ yếu là kiểm thử tải, integration/RBAC test, triển khai production và hoàn thiện UX; migration, validation, Swagger, health check, backup/restore và CI đã được bổ sung.

### 9. Endpoint kỹ thuật

- Swagger UI: `http://localhost:5165/swagger`
- Health check: `http://localhost:5165/health`
- Backup: `powershell -ExecutionPolicy Bypass -File .\Database\backup-mysql.ps1`
- Restore: `powershell -ExecutionPolicy Bypass -File .\Database\restore-mysql.ps1 -InputFile <file.sql>`
- Smoke/RBAC/performance: `powershell -ExecutionPolicy Bypass -File .\Tests\api-smoke.ps1`
- Docker: `docker compose up --build`

### 8. Đánh giá cuối cùng

- “Đã đáp ứng phần lớn yêu cầu trong file phân tích”: Đúng
- “Đã hoàn thành toàn bộ nếu coi đây là hệ thống thực tế”: Chưa
- “Đã đủ cho bài tập / demo / bảo vệ / môn học”: Đủ và tốt

## Reset dữ liệu demo

Dừng server, backup database, xóa/tạo lại database `webnangcao` trong phpMyAdmin, rồi chạy `dotnet run`. Migration sẽ tạo lại schema và dữ liệu mẫu ban đầu.
