param(
    [string]$Output = "./artifacts/migrations/coursecore-migration.sql"
)

$ErrorActionPreference = "Stop"

$repositoryRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$outputPath = Join-Path $repositoryRoot $Output
$outputDirectory = Split-Path -Parent $outputPath

New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null

Push-Location $repositoryRoot
try {
    dotnet ef migrations script `
        --context CourseCoreDbContext `
        --idempotent `
        --output $outputPath
}
finally {
    Pop-Location
}
