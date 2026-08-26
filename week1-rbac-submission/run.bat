@echo off
setlocal

echo ========================================
echo Week 1 RBAC API runner
echo ========================================
echo.

where dotnet >nul 2>nul
if errorlevel 1 (
  echo ERROR: Khong tim thay dotnet.
  echo Hay cai .NET 10 SDK truoc khi chay bai nay.
  echo.
  pause
  exit /b 1
)

cd /d "%~dp0week1-rbac"
if errorlevel 1 (
  echo ERROR: Khong vao duoc thu muc week1-rbac.
  pause
  exit /b %errorlevel%
)

set EF_DLL=%USERPROFILE%\.nuget\packages\dotnet-ef\10.0.10\tools\net8.0\any\dotnet-ef.dll

echo Restoring local tools...
dotnet tool restore >nul 2>nul
if errorlevel 1 (
  if exist "%EF_DLL%" (
    echo WARNING: dotnet tool restore bi loi, nhung da tim thay dotnet-ef trong NuGet cache.
    echo Script se tiep tuc bang file local: %EF_DLL%
  ) else (
    echo.
    echo ERROR: Khong restore duoc dotnet-ef.
    echo Kiem tra ket noi mang hoac thu lai lenh: dotnet tool restore
    echo.
    pause
    exit /b %errorlevel%
  )
)

echo Restoring packages...
dotnet restore .\Week1.Rbac.Api\Week1.Rbac.Api.csproj
if errorlevel 1 (
  echo.
  echo ERROR: Khong restore duoc package NuGet.
  echo.
  pause
  exit /b %errorlevel%
)

echo Checking PostgreSQL port...
for /f %%P in ('powershell -NoProfile -ExecutionPolicy Bypass -Command "$ports=5433,5432,5434,5435; foreach($p in $ports){ try { $c=[Net.Sockets.TcpClient]::new(); $iar=$c.BeginConnect('127.0.0.1',$p,$null,$null); if($iar.AsyncWaitHandle.WaitOne(300) -and $c.Connected){ $c.Close(); $p; exit 0 }; $c.Close() } catch {} }; exit 1"') do set DB_PORT=%%P
if not defined DB_PORT (
  echo.
  echo PostgreSQL not detected on standard local ports. Trying local fallback setup on port 5433...
  call "%~dp0setup-postgres-db.bat"
  for /f %%P in ('powershell -NoProfile -ExecutionPolicy Bypass -Command "$ports=5433,5432,5434,5435; foreach($p in $ports){ try { $c=[Net.Sockets.TcpClient]::new(); $iar=$c.BeginConnect('127.0.0.1',$p,$null,$null); if($iar.AsyncWaitHandle.WaitOne(300) -and $c.Connected){ $c.Close(); $p; exit 0 }; $c.Close() } catch {} }; exit 1"') do set DB_PORT=%%P
)
if not defined DB_PORT (
  echo.
  echo ERROR: Khong tim thay PostgreSQL dang chay tren cac cong 5433, 5432, 5434, 5435.
  echo Loi ban gap la do database port bi tu choi ket noi, khong phai do cong website.
  echo.
  echo Cach sua:
  echo   1. Cai va bat PostgreSQL.
  echo   2. Tao user/database bang pgAdmin hoac psql:
  echo      CREATE USER week1_app WITH PASSWORD 'week1_secret';
  echo      CREATE DATABASE week1_rbac OWNER week1_app;
  echo   3. Chay lai run.bat.
  echo.
  pause
  exit /b 1
)
echo PostgreSQL detected at localhost:%DB_PORT%
set ConnectionStrings__Default=Host=localhost;Port=%DB_PORT%;Database=week1_rbac;Username=week1_app;Password=week1_secret

echo Applying EF Core migrations...
set EF_CMD=dotnet tool run dotnet-ef
%EF_CMD% --version >nul 2>nul
if errorlevel 1 (
  if exist "%EF_DLL%" (
    set EF_CMD=dotnet "%EF_DLL%"
  )
)

%EF_CMD% --project .\Week1.Rbac.Api\Week1.Rbac.Api.csproj database update
if errorlevel 1 (
  echo.
  echo ERROR: Migration failed.
  echo Hay dam bao PostgreSQL dang chay o localhost:5432 va database da duoc tao:
  echo   CREATE USER week1_app WITH PASSWORD 'week1_secret';
  echo   CREATE DATABASE week1_rbac OWNER week1_app;
  echo.
  pause
  exit /b %errorlevel%
)

echo Checking free API port...
for /f %%P in ('powershell -NoProfile -ExecutionPolicy Bypass -Command "$ports=5080,5081,5082,5083,5084,5085,5000,5001,8080,8081; foreach($p in $ports){ try { $l=[Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback,$p); $l.Start(); $l.Stop(); $p; exit 0 } catch {} }; exit 1"') do set API_PORT=%%P
if not defined API_PORT (
  echo.
  echo ERROR: Khong tim thay cong API trong tren danh sach 5080-5085, 5000-5001, 8080-8081.
  echo Hay tat ung dung dang chiem cong roi chay lai.
  echo.
  pause
  exit /b 1
)

echo.
echo Starting API at http://localhost:%API_PORT%
echo Swagger UI: http://localhost:%API_PORT%/swagger
echo.
echo Khi thay dong "Now listening on", mo trinh duyet vao:
echo   http://localhost:%API_PORT%/swagger
echo.
cd /d "%~dp0week1-rbac\Week1.Rbac.Api"
if errorlevel 1 (
  echo ERROR: Khong vao duoc thu muc project API.
  pause
  exit /b %errorlevel%
)
dotnet run --urls http://localhost:%API_PORT%

echo.
pause
