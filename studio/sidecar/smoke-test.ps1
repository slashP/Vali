# Pipes a fixed request sequence through Vali.Host over stdin/stdout and prints the responses.
# Build first: dotnet build studio/sidecar/Vali.Host -c Release
$exe = Join-Path $PSScriptRoot "Vali.Host\bin\Release\net10.0\Vali.Host.exe"
if (-not (Test-Path $exe)) {
    $exe = Join-Path $PSScriptRoot "Vali.Host\bin\Debug\net10.0\Vali.Host.exe"
}
if (-not (Test-Path $exe)) {
    Write-Error "Vali.Host.exe not found — run: dotnet build studio/sidecar/Vali.Host -c Release"
    exit 1
}

$requests = @(
    '{"id":"1","cmd":"ping"}'
    '{"id":"2","cmd":"expressionCheck","params":{"expression":"Year gte 2020 and Buildings100 eq 0"}}'
    '{"id":"3","cmd":"expressionCheck","params":{"expression":"Yearr gte 2020"}}'
    '{"id":"4","cmd":"expressionCheck","params":{"expression":"$$rural","namedExpressions":{"$$rural":"Buildings200 eq 0 and Roads100 eq 1"}}}'
    '{"id":"5","cmd":"countries"}'
    '{"id":"6","cmd":"subdivisions","params":{"countryCode":"NO"}}'
    '{"id":"7","cmd":"validate","params":{"json":"{\"countryCodes\":[\"NO\"],\"distributionStrategy\":{\"key\":\"FixedCountByMaxMinDistance\",\"locationCountGoal\":1000,\"minMinDistance\":100}}"}}'
    '{"id":"8","cmd":"validate","params":{"json":"{\"countryCodes\":[\"XX\"]}"}}'
    '{"id":"9","cmd":"dataStatus"}'
    '{"id":"10","cmd":"nope"}'
)

$requests | & $exe
