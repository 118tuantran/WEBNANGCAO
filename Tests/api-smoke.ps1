param([string]$BaseUrl = 'http://localhost:5165')
$ErrorActionPreference = 'Stop'
function Assert-Status($actual, $expected, $name) { if ($actual -ne $expected) { throw "$name expected HTTP $expected, got $actual" } }
function Login($username, $password) {
    $session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
    $response = Invoke-WebRequest "$BaseUrl/api/auth/login" -Method Post -ContentType 'application/json' -Body (@{ username = $username; password = $password } | ConvertTo-Json) -WebSession $session -UseBasicParsing
    Assert-Status $response.StatusCode 200 "Login $username"
    return $session
}
$health = Invoke-WebRequest "$BaseUrl/health" -UseBasicParsing
Assert-Status $health.StatusCode 200 'Health check'
$swagger = Invoke-WebRequest "$BaseUrl/swagger/index.html" -UseBasicParsing
Assert-Status $swagger.StatusCode 200 'Swagger'
$manager = Login 'manager' 'Manager@123'
$admin = Login 'admin' 'Admin@123'
$staff = Login 'staff' 'Staff@123'
Assert-Status (Invoke-WebRequest "$BaseUrl/api/admin/products" -WebSession $manager -UseBasicParsing).StatusCode 200 'Manager master data access'
try { Invoke-WebRequest "$BaseUrl/api/admin/products" -WebSession $admin -UseBasicParsing | Out-Null; throw 'Admin unexpectedly accessed manager master data' } catch { if ($_.Exception.Response.StatusCode.value__ -ne 403) { throw } }
try { Invoke-WebRequest "$BaseUrl/api/warehouse/pending/receipts" -WebSession $staff -UseBasicParsing | Out-Null; throw 'Staff unexpectedly approved workflow access' } catch { if ($_.Exception.Response.StatusCode.value__ -ne 403) { throw } }
$times = 1..20 | ForEach-Object { $start = Get-Date; Invoke-WebRequest "$BaseUrl/api/warehouse/products" -WebSession $manager -UseBasicParsing | Out-Null; ((Get-Date) - $start).TotalMilliseconds }
$average = ($times | Measure-Object -Average).Average
Write-Output "API smoke passed. Average products query: $([math]::Round($average, 2)) ms"
