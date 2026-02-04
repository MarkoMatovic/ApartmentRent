# ML Endpoints Test Script
# Uputstvo: Uloguj se u aplikaciju, otvori Developer Tools (F12), 
# idi na Application > Local Storage, kopiraj token i zalepi ga ispod

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "   ML Endpoints Test Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Ovde unesi token nakon što se uloguješ u aplikaciju
$TOKEN = Read-Host "Unesi JWT token (iz Local Storage u browseru)"

if ([string]::IsNullOrWhiteSpace($TOKEN)) {
    Write-Host "❌ Token nije unet! Izlazim..." -ForegroundColor Red
    exit
}

$API_BASE = "http://localhost:5197/api/v1/ml"
$headers = @{
    "Authorization" = "Bearer $TOKEN"
    "Content-Type" = "application/json"
}

Write-Host "`n1️⃣  Proveravam da li je model treniran..." -ForegroundColor Yellow
try {
    $modelStatus = Invoke-RestMethod -Uri "$API_BASE/is-model-trained" -Method Get
    Write-Host "   Status: $($modelStatus.isTrained)" -ForegroundColor Green
} catch {
    Write-Host "   ❌ Greška: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n2️⃣  Treniram model sa 54 apartmana..." -ForegroundColor Yellow
Write-Host "   (Ovo može potrajati 10-30 sekundi...)" -ForegroundColor Gray
try {
    $trainResult = Invoke-RestMethod -Uri "$API_BASE/train-price-model" -Method Post -Headers $headers
    Write-Host "   ✅ Treniranje uspešno!" -ForegroundColor Green
    Write-Host "`n   📊 Metrike modela:" -ForegroundColor Cyan
    Write-Host "   - R² Score: $([math]::Round($trainResult.rSquared, 4))" -ForegroundColor White
    Write-Host "   - Mean Absolute Error: $([math]::Round($trainResult.meanAbsoluteError, 2))" -ForegroundColor White
    Write-Host "   - Root Mean Squared Error: $([math]::Round($trainResult.rootMeanSquaredError, 2))" -ForegroundColor White
    Write-Host "   - Training Samples: $($trainResult.trainingSampleCount)" -ForegroundColor White
    Write-Host "   - Last Trained: $($trainResult.lastTrainedDate)" -ForegroundColor White
} catch {
    Write-Host "   ❌ Greška pri treniranju: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.ErrorDetails) {
        Write-Host "   Detalji: $($_.ErrorDetails.Message)" -ForegroundColor Red
    }
}

Write-Host "`n3️⃣  Testiram price prediction..." -ForegroundColor Yellow
$testApartment = @{
    sizeSquareMeters = 75
    numberOfRooms = 2
    city = "Belgrade"
    isFurnished = $false
    hasBalcony = $true
    hasParking = $false
    hasElevator = $true
    hasAirCondition = $false
    hasInternet = $true
    isPetFriendly = $false
    isSmokingAllowed = $false
    apartmentType = 0
} | ConvertTo-Json

try {
    $prediction = Invoke-RestMethod -Uri "$API_BASE/predict-price" -Method Post -Body $testApartment -ContentType "application/json"
    Write-Host "   ✅ Predikcija uspešna!" -ForegroundColor Green
    Write-Host "`n   🏠 Test apartman:" -ForegroundColor Cyan
    Write-Host "   - Veličina: 75m²" -ForegroundColor White
    Write-Host "   - Sobe: 2" -ForegroundColor White
    Write-Host "   - Grad: Belgrade" -ForegroundColor White
    Write-Host "   - Balkon: Da, Lift: Da, Internet: Da" -ForegroundColor White
    Write-Host "`n   💰 Rezultat:" -ForegroundColor Cyan
    Write-Host "   - Predviđena cena: $($prediction.predictedPrice) EUR" -ForegroundColor Green
    Write-Host "   - Confidence Score: $($prediction.confidenceScore)%" -ForegroundColor Green
    Write-Host "   - Poruka: $($prediction.message)" -ForegroundColor White
} catch {
    Write-Host "   ❌ Greška: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n4️⃣  Proveravam metrike modela..." -ForegroundColor Yellow
try {
    $metrics = Invoke-RestMethod -Uri "$API_BASE/model-metrics" -Method Get -Headers $headers
    Write-Host "   ✅ Metrike učitane!" -ForegroundColor Green
    Write-Host "`n   📈 Finalne metrike:" -ForegroundColor Cyan
    Write-Host "   - R² Score: $([math]::Round($metrics.rSquared, 4))" -ForegroundColor White
    Write-Host "   - MAE: $([math]::Round($metrics.meanAbsoluteError, 2))" -ForegroundColor White
    Write-Host "   - RMSE: $([math]::Round($metrics.rootMeanSquaredError, 2))" -ForegroundColor White
} catch {
    Write-Host "   ❌ Greška: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "   Test završen!" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
