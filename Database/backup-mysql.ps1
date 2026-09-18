param(
    [string]$OutputDirectory = "$PSScriptRoot\backups",
    [string]$HostName = $(if ($env:WAREHOUSE_DB_HOST) { $env:WAREHOUSE_DB_HOST } else { '127.0.0.1' }),
    [int]$Port = $(if ($env:WAREHOUSE_DB_PORT) { [int]$env:WAREHOUSE_DB_PORT } else { 3306 }),
    [string]$Database = $(if ($env:WAREHOUSE_DB_NAME) { $env:WAREHOUSE_DB_NAME } else { 'webnangcao' }),
    [string]$User = $(if ($env:WAREHOUSE_DB_USER) { $env:WAREHOUSE_DB_USER } else { 'root' })
)
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$output = Join-Path $OutputDirectory "$Database-$timestamp.sql"
$mysqlDump = Join-Path ${env:ProgramFiles} 'MySQL\MySQL Server 8.0\bin\mysqldump.exe'
if (-not (Test-Path $mysqlDump)) { $mysqlDump = 'C:\xampp\mysql\bin\mysqldump.exe' }
& $mysqlDump --host=$HostName --port=$Port --user=$User --single-transaction --routines --triggers $Database | Set-Content -Encoding utf8 $output
if ($LASTEXITCODE -ne 0) { throw "mysqldump failed with exit code $LASTEXITCODE" }
Write-Output "Backup created: $output"
