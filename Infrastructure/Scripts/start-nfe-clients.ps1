param(
    [string]$Address = "127.0.0.1",
    [ValidateRange(1, 65535)][int]$Port = 7980,
    [ValidateRange(1, 4)][int]$Clients = 4
)

$ErrorActionPreference = "Stop"
$parsedAddress = $null
if (-not [System.Net.IPAddress]::TryParse($Address, [ref]$parsedAddress) -or
    $parsedAddress.AddressFamily -ne [System.Net.Sockets.AddressFamily]::InterNetwork) {
    throw "Address debe ser una IPv4, por ejemplo 127.0.0.1 o la IP Host-Only de Debian."
}

$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "../.."))
$executable = Join-Path $projectRoot "Builds/NFE-WindowsClient/NetworkingLabNFEClient.exe"
if (-not (Test-Path -LiteralPath $executable -PathType Leaf)) {
    throw "Primero genera Networking Lab > NFE > Build Windows Client en Unity."
}

$logsDirectory = Join-Path $projectRoot "Logs/NFE"
New-Item -ItemType Directory -Path $logsDirectory -Force | Out-Null
$runId = Get-Date -Format "yyyyMMdd-HHmmss"
for ($index = 1; $index -le $Clients; $index++) {
    $logPath = Join-Path $logsDirectory "client-$runId-$index.log"
    $arguments = '-nfe -client -address "{0}" -port {1} -logFile "{2}"' -f $parsedAddress, $Port, $logPath
    Start-Process -FilePath $executable -ArgumentList $arguments -WorkingDirectory (Split-Path $executable)
}
Write-Host "Iniciados $Clients clientes hacia ${Address}:$Port. Logs: $logsDirectory"
