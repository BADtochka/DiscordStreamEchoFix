Write-Host "Discord Audio Guard - Build" -ForegroundColor Cyan
Write-Host ""

# Kill processes
Get-Process -Name "DiscordStreamEchoFix", "dotnet" -ErrorAction SilentlyContinue | 
    ForEach-Object { 
        Write-Host "Shutdown process: $($_.Name) (PID: $($_.Id))" -ForegroundColor Yellow
        Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue 
    }
Start-Sleep -Seconds 1

# Build
dotnet clean
dotnet build -c Release

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n✓ Build successful" -ForegroundColor Green
    Write-Host "Running app..." -ForegroundColor Green
    dotnet run --project DiscordStreamEchoFix.csproj
} else {
    Write-Host "`n✗ Build error" -ForegroundColor Red
}
