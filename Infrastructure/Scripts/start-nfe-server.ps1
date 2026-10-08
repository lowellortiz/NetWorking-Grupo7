param([ValidateRange(1, 65535)][int]$Port = 7980)

$ErrorActionPreference = "Stop"
$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "../.."))
$executable = Join-Path $projectRoot "Builds/NFE-WindowsServer/NetworkingLabNFEServer.exe"
if (-not (Test-Path -LiteralPath $executable -PathType Leaf)) {
    throw "Primero genera Networking Lab > NFE > Build Windows Server en Unity."
}

$logsDirectory = Join-Path $projectRoot "Logs/NFE"
New-Item -ItemType Directory -Path $logsDirectory -Force | Out-Null
$logPath = Join-Path $logsDirectory ("server-" + (Get-Date -Format "yyyyMMdd-HHmmss") + ".log")
$arguments = '-batchmode -nographics -nfe -port {0} -logFile "{1}"' -f $Port, $logPath
$process = Start-Process -FilePath $executable -ArgumentList $arguments -WorkingDirectory (Split-Path $executable) -PassThru
Write-Host "Servidor NFE iniciado. PID: $($process.Id). UDP: $Port. Log: $logPath"
