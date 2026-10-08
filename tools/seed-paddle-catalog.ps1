<#
  Seed the Paddle SANDBOX catalog for TuRentaj.

  Creates 6 products and 12 one-time EUR prices via the Paddle API, then prints the
  planId -> pri_... mapping (and ready-to-paste config) so you can wire it up.

  The API key is read from the PADDLE_API_KEY environment variable — it is never
  hard-coded and never printed.

  USAGE (PowerShell, in this folder):
      $env:PADDLE_API_KEY = "pdl_sdbx_...your sandbox key..."
      ./seed-paddle-catalog.ps1

  Re-running creates DUPLICATE products/prices (Paddle has no upsert). Run once;
  if you need to redo it, archive the old products in the dashboard first.
#>

$ErrorActionPreference = 'Stop'

$apiKey = $env:PADDLE_API_KEY
if ([string]::IsNullOrWhiteSpace($apiKey)) {
    Write-Error "PADDLE_API_KEY is not set. Run:  `$env:PADDLE_API_KEY = 'pdl_sdbx_...'  then re-run."
    exit 1
}

$baseUrl = 'https://sandbox-api.paddle.com'   # sandbox; change to https://api.paddle.com for live
$headers = @{
    Authorization  = "Bearer $apiKey"
    'Content-Type' = 'application/json'
}

# Catalog: one entry per product, each with its one-time prices (planId + amount in
# the lowest denomination as a string, e.g. EUR 4.99 = "499").
$catalog = @(
    @{ product = 'Analitika';              prices = @(
        @{ planId = 'analytics-monthly'; desc = 'Analitika - 30 dana';   amount = '499'  },
        @{ planId = 'analytics-yearly';  desc = 'Analitika - 12 meseci'; amount = '4999' }) },
    @{ product = 'Tokeni';                 prices = @(
        @{ planId = 'tokens-10';  desc = '10 tokena';  amount = '299'  },
        @{ planId = 'tokens-50';  desc = '50 tokena';  amount = '999'  },
        @{ planId = 'tokens-150'; desc = '150 tokena'; amount = '2499' }) },
    @{ product = 'Istaknut oglas';         prices = @(
        @{ planId = 'featured-7';  desc = 'Istaknut 7 dana';  amount = '999'  },
        @{ planId = 'featured-30'; desc = 'Istaknut 30 dana'; amount = '2999' }) },
    @{ product = 'Objava oglasa';          prices = @(
        @{ planId = 'listing-1'; desc = '1 oglas';  amount = '500'  },
        @{ planId = 'listing-3'; desc = '3 oglasa'; amount = '1500' },
        @{ planId = 'listing-5'; desc = '5 oglasa'; amount = '2500' }) },
    @{ product = 'Boost profila cimera';   prices = @(
        @{ planId = 'boost-7'; desc = 'Boost 7 dana'; amount = '200' }) },
    @{ product = 'Priority Inbox';         prices = @(
        @{ planId = 'priority-30'; desc = 'Priority 30 dana'; amount = '200' }) }
)

$mapping = [ordered]@{}

foreach ($item in $catalog) {
    Write-Host "Creating product: $($item.product) ..." -ForegroundColor Cyan

    $productBody = @{
        name         = $item.product
        tax_category = 'standard'
        type         = 'standard'
    } | ConvertTo-Json

    $productResp = Invoke-RestMethod -Method Post -Uri "$baseUrl/products" -Headers $headers -Body $productBody
    $productId = $productResp.data.id
    Write-Host "  product id: $productId" -ForegroundColor DarkGray

    foreach ($p in $item.prices) {
        $priceBody = @{
            product_id  = $productId
            description = $p.desc
            unit_price  = @{ amount = $p.amount; currency_code = 'EUR' }
            quantity    = @{ minimum = 1; maximum = 1 }
            # No billing_cycle => one-time price (no subscription, no trial).
        } | ConvertTo-Json -Depth 5

        $priceResp = Invoke-RestMethod -Method Post -Uri "$baseUrl/prices" -Headers $headers -Body $priceBody
        $priceId = $priceResp.data.id
        $mapping[$p.planId] = $priceId
        Write-Host ("  {0,-20} -> {1}" -f $p.planId, $priceId) -ForegroundColor Green
    }
}

Write-Host "`n===== planId -> Paddle Price ID =====" -ForegroundColor Yellow
foreach ($k in $mapping.Keys) { Write-Host ("{0} = {1}" -f $k, $mapping[$k]) }

Write-Host "`n===== user-secrets commands (run in LandlordApp folder) =====" -ForegroundColor Yellow
foreach ($k in $mapping.Keys) {
    Write-Host ("dotnet user-secrets set `"Paddle:PriceIds:{0}`" `"{1}`"" -f $k, $mapping[$k])
}

Write-Host "`n===== appsettings PriceIds block =====" -ForegroundColor Yellow
$json = ($mapping.GetEnumerator() | ForEach-Object { "      `"$($_.Key)`": `"$($_.Value)`"" }) -join ",`n"
Write-Host "`"PriceIds`": {`n$json`n}"

Write-Host "`nDone. Copy the mapping above." -ForegroundColor Cyan
