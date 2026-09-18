param(
    [Parameter(Mandatory = $true)][string]$InputFile,
    [string]$HostName = $(if ($env:WAREHOUSE_DB_HOST) { $env:WAREHOUSE_DB_HOST } else { '127.0.0.1' }),
    [int]$Port = $(if ($env:WAREHOUSE_DB_PORT) { [int]$env:WAREHOUSE_DB_PORT } else { 3306 }),
    [string]$Database = $(if ($env:WAREHOUSE_DB_NAME) { $env:WAREHOUSE_DB_NAME } else { 'webnangcao' }),
    [string]$User = $(if ($env:WAREHOUSE_DB_USER) { $env:WAREHOUSE_DB_USER } else { 'root' })
)
if (-not (Test-Path $InputFile)) { throw "Backup file not found: $InputFile" }
$mysql = 'C:\xampp\mysql\bin\mysql.exe'
Get-Content -Raw $InputFile | & $mysql --host=$HostName --port=$Port --user=$User $Database
if ($LASTEXITCODE -ne 0) { throw "mysql restore failed with exit code $LASTEXITCODE" }
Write-Output "Backup restored into database: $Database"
