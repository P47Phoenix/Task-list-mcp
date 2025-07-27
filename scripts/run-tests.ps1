param(
    [string]$Configuration = "Release",
    [string]$TestCategory = "All",
    [switch]$Coverage,
    [switch]$Parallel
)

Write-Host "Task List MCP Test Runner" -ForegroundColor Green
Write-Host "=========================" -ForegroundColor Green
Write-Host ""

$testProjects = @(
    "tests/TaskListMcp.Tests.Unit",
    "tests/TaskListMcp.Tests.Integration",
    "tests/TaskListMcp.Tests.Performance"
)

$testArgs = @(
    "--configuration", $Configuration,
    "--logger", "trx",
    "--logger", "console;verbosity=normal",
    "--results-directory", "test-results"
)

if ($Coverage) {
    $testArgs += @(
        "--collect", "XPlat Code Coverage",
        "--settings", "coverlet.runsettings"
    )
}

if ($Parallel) {
    $testArgs += @("--parallel")
}

# Create results directory
New-Item -ItemType Directory -Path "test-results" -Force | Out-Null

switch ($TestCategory) {
    "Unit" {
        Write-Host "Running Unit Tests..." -ForegroundColor Yellow
        dotnet test $testProjects[0] @testArgs
    }
    "Integration" {
        Write-Host "Running Integration Tests..." -ForegroundColor Yellow
        dotnet test $testProjects[1] @testArgs
    }
    "Performance" {
        Write-Host "Running Performance Tests..." -ForegroundColor Yellow
        dotnet test $testProjects[2] @testArgs
    }
    "All" {
        foreach ($project in $testProjects) {
            Write-Host "Running tests in $project..." -ForegroundColor Yellow
            dotnet test $project @testArgs
            if ($LASTEXITCODE -ne 0) {
                Write-Host "Tests failed in $project" -ForegroundColor Red
                exit $LASTEXITCODE
            }
        }
    }
}

if ($Coverage) {
    Write-Host ""
    Write-Host "Generating Coverage Report..." -ForegroundColor Yellow
    
    # Install reportgenerator if not present
    $reportGeneratorExists = Get-Command reportgenerator -ErrorAction SilentlyContinue
    if (-not $reportGeneratorExists) {
        Write-Host "Installing dotnet-reportgenerator-globaltool..." -ForegroundColor Cyan
        dotnet tool install -g dotnet-reportgenerator-globaltool
    }
    
    reportgenerator -reports:"test-results/**/coverage.cobertura.xml" -targetdir:"test-results/coverage" -reporttypes:Html
    Write-Host "Coverage report generated in test-results/coverage/" -ForegroundColor Green
    Write-Host "Open test-results/coverage/index.html to view the report" -ForegroundColor Green
}

Write-Host ""
Write-Host "Test execution completed!" -ForegroundColor Green
