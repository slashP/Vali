# The drift firewall: proves Vali.Host's re-orchestrated generation produces byte-identical
# output to the real Vali CLI under VALI_DETERMINISTIC=1. Run after upstream Vali changes.
# Usage: .\parity-test.ps1 -DefinitionPath C:\maps\IE.json
param(
    [Parameter(Mandatory = $true)] [string] $DefinitionPath
)

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$exe = Join-Path $PSScriptRoot "Vali.Host\bin\Release\net10.0\Vali.Host.exe"
if (-not (Test-Path $exe)) {
    Write-Error "Vali.Host.exe not found — run: dotnet build studio/sidecar/Vali.Host -c Release"
    exit 1
}

$work = Join-Path ([System.IO.Path]::GetTempPath()) ("vali-parity-" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $work | Out-Null
Copy-Item $DefinitionPath (Join-Path $work "host.json")
Copy-Item $DefinitionPath (Join-Path $work "cli.json")

$env:VALI_DETERMINISTIC = "1"
try {
    $hostDef = (Join-Path $work "host.json").Replace('\', '\\')
    '{"id":"p","cmd":"generate","params":{"definitionPath":"' + $hostDef + '"}}' | & $exe | Out-Null
    dotnet run --project (Join-Path $repoRoot "src\Vali") --framework net10.0 -c Release -- generate --file (Join-Path $work "cli.json") | Out-Null

    $hostOut = Join-Path $work "host-locations.json"
    $cliOut = Join-Path $work "cli-locations.json"
    if (-not (Test-Path $hostOut)) { Write-Error "Sidecar produced no output."; exit 1 }
    if (-not (Test-Path $cliOut)) { Write-Error "CLI produced no output."; exit 1 }

    $hostHash = (Get-FileHash $hostOut -Algorithm SHA256).Hash
    $cliHash = (Get-FileHash $cliOut -Algorithm SHA256).Hash
    if ($hostHash -eq $cliHash) {
        Write-Host "PARITY OK: $hostHash"
        exit 0
    }

    Write-Error "PARITY FAILED: sidecar $hostHash != cli $cliHash (outputs kept in $work)"
    exit 1
}
finally {
    $env:VALI_DETERMINISTIC = $null
    if ($hostHash -and ($hostHash -eq $cliHash)) { Remove-Item -Recurse -Force $work -Confirm:$false }
}
