# k6 Performance Tests

## Prerequisites

Install k6: https://grafana.com/docs/k6/latest/get-started/installation/

```bash
# Windows (Chocolatey)
choco install k6

# macOS
brew install k6

# Linux
sudo gpg --no-default-keyring --keyring /usr/share/keyrings/k6-archive-keyring.gpg \
         --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys C5AD17C747E3415A3642D57D77C6C491D6AC1D69
echo "deb [signed-by=/usr/share/keyrings/k6-archive-keyring.gpg] https://dl.k6.io/deb stable main" \
     | sudo tee /etc/apt/sources.list.d/k6.list
sudo apt-get update && sudo apt-get install k6
```

## Test scripts

| File | What it tests | Default target VUs |
|---|---|---|
| `apartments-load.js` | Apartment list + detail (cache hit paths) | 30 |
| `auth-load.js` | Login + rate limiter behaviour | 10 |
| `signalr-chat-spike.js` | SignalR negotiate under sudden spike | 100 |

## Running locally

```bash
# Start the API first
cd ../LandlordApp && dotnet run

# Run a test (in another terminal)
cd k6
k6 run apartments-load.js
k6 run --env BASE_URL=http://localhost:5000 apartments-load.js

# Run auth test with real credentials
k6 run --env TEST_EMAIL=you@example.com --env TEST_PASSWORD=Pass123! auth-load.js

# Run SignalR spike with a JWT
k6 run --env TOKEN=<your-jwt> signalr-chat-spike.js
```

## SLOs enforced by thresholds

- `p(95) < 500 ms` for all HTTP requests
- Error rate `< 1 %`
- List-specific: `p(95) < 400 ms`
- Detail-specific: `p(95) < 300 ms`

## CI integration (GitHub Actions example)

```yaml
- name: k6 load test
  uses: grafana/k6-action@v0.3.1
  with:
    filename: k6/apartments-load.js
  env:
    BASE_URL: http://localhost:5000
```
