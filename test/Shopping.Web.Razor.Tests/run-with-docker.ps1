param(
    [switch]$Coverage = $true,
    [int]$DockerStartupTimeoutSeconds = 120
)

$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptDir "..\..")

function Test-DockerReady {
    try {
        docker info | Out-Null
        return $true
    }
    catch {
        return $false
    }
}

function Start-DockerDesktopIfNeeded {
    if (Test-DockerReady) {
        return
    }

    $dockerDesktop = "C:\Program Files\Docker\Docker\Docker Desktop.exe"
    if (Test-Path $dockerDesktop) {
        Start-Process -FilePath $dockerDesktop | Out-Null
    }

    $attempts = [Math]::Max(1, [Math]::Floor($DockerStartupTimeoutSeconds / 2))
    for ($i = 0; $i -lt $attempts; $i++) {
        if (Test-DockerReady) {
            return
        }
    }

    throw "Docker is not ready. Start Docker Desktop and retry."
}

Push-Location $repoRoot
try {
    Start-DockerDesktopIfNeeded

    Push-Location (Join-Path $repoRoot "src")
    try {
        docker compose -f docker-compose.yml -f docker-compose.override.yml up -d catalogdb basketdb distributedcache orderingdb messagebroker discount.grpc catalog.api basket.api ordering.api yarpapigateway
    }
    finally {
        Pop-Location
    }

    $env:SHOPPING_REQUIRE_DOCKER_TESTS = "true"

    if ($Coverage) {
        dotnet test test/Shopping.Web.Razor.Tests/Shopping.Web.Razor.Tests.csproj --collect:"XPlat Code Coverage"
    }
    else {
        dotnet test test/Shopping.Web.Razor.Tests/Shopping.Web.Razor.Tests.csproj
    }
}
finally {
    Pop-Location
}
