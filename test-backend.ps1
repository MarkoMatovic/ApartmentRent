# Test backend API directly
Write-Host "Testing Backend API..." -ForegroundColor Cyan

# Test 1: No filters
Write-Host "`n1. Test without filters:" -ForegroundColor Yellow
try {
    $response1 = Invoke-RestMethod -Uri "https://localhost:5002/api/v1/rent/get-all-apartments" -Method Get -SkipCertificateCheck
    Write-Host "   Total apartments: $($response1.Count)" -ForegroundColor Green
} catch {
    Write-Host "   ERROR: $_" -ForegroundColor Red
}

# Test 2: With minRent and maxRent (camelCase)
Write-Host "`n2. Test with camelCase params (minRent=300&maxRent=500):" -ForegroundColor Yellow
try {
    $response2 = Invoke-RestMethod -Uri "https://localhost:5002/api/v1/rent/get-all-apartments?minRent=300&maxRent=500" -Method Get -SkipCertificateCheck
    Write-Host "   Total apartments: $($response2.Count)" -ForegroundColor Green
    if ($response2.Count -gt 0) {
        $response2 | Select-Object -First 3 | ForEach-Object {
            Write-Host "   - [$($_.apartmentId)] $($_.title) - $($_.rent) EUR" -ForegroundColor White
        }
    }
} catch {
    Write-Host "   ERROR: $_" -ForegroundColor Red
}

# Test 3: With MinRent and MaxRent (PascalCase)
Write-Host "`n3. Test with PascalCase params (MinRent=300&MaxRent=500):" -ForegroundColor Yellow
try {
    $response3 = Invoke-RestMethod -Uri "https://localhost:5002/api/v1/rent/get-all-apartments?MinRent=300&MaxRent=500" -Method Get -SkipCertificateCheck
    Write-Host "   Total apartments: $($response3.Count)" -ForegroundColor Green
    if ($response3.Count -gt 0) {
        $response3 | Select-Object -First 3 | ForEach-Object {
            Write-Host "   - [$($_.apartmentId)] $($_.title) - $($_.rent) EUR" -ForegroundColor White
        }
    }
} catch {
    Write-Host "   ERROR: $_" -ForegroundColor Red
}

Write-Host "`nDone!" -ForegroundColor Cyan
