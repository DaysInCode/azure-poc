# Complete Azure POC Environment Setup Script
Write-Host "Starting Azure POC Environment Setup..." -ForegroundColor Cyan

# Step 1: Start Docker containers
Write-Host "`nStarting Docker containers..." -ForegroundColor Yellow
docker-compose up -d

# Step 2: Wait for Cosmos DB emulator to be ready (about 90 seconds)
Write-Host "`nWaiting for Cosmos DB emulator to start (90 seconds)..." -ForegroundColor Yellow
Write-Host "This may take a while as the container needs to initialize..." -ForegroundColor DarkYellow
$startTime = Get-Date
$waitTime = New-TimeSpan -Seconds 90
$endTime = $startTime + $waitTime

# Show a simple progress bar while waiting
$progressBar = 0
$progressBarWidth = 20
while ((Get-Date) -lt $endTime) {
    $elapsed = (Get-Date) - $startTime
    $percentComplete = [Math]::Min(100, [int](($elapsed.TotalSeconds / $waitTime.TotalSeconds) * 100))
    $progressBar = [Math]::Min($progressBarWidth, [int](($percentComplete / 100) * $progressBarWidth))
    
    Write-Host -NoNewline "`rProgress: ["
    Write-Host -NoNewline -ForegroundColor Green ("=" * $progressBar)
    Write-Host -NoNewline -ForegroundColor DarkGray ("-" * ($progressBarWidth - $progressBar))
    Write-Host -NoNewline "] $percentComplete% complete"
    
    Start-Sleep -Milliseconds 500
}
Write-Host "`n" # Add a newline after progress bar

# Step 3: Setup the Cosmos DB certificate
Write-Host "`nSetting up Cosmos DB certificate..." -ForegroundColor Yellow
& .\setup-certificates.ps1

# Step 4: Initialize the Cosmos DB with databases, containers, and documents
Write-Host "`nInitializing databases, containers, and Service Bus topics..." -ForegroundColor Yellow
& .\initialize-cosmosdb.bat

# Step 5: Verify services are running
Write-Host "`nVerifying services..." -ForegroundColor Yellow
$services = @(
    @{Name = "Azurite (Blob)"; Url = "http://localhost:10000"; Type = "HTTP"},
    @{Name = "Azurite (Queue)"; Url = "http://localhost:10001"; Type = "HTTP"},
    @{Name = "Azurite (Table)"; Url = "http://localhost:10002"; Type = "HTTP"},
    @{Name = "Cosmos DB"; Url = "https://localhost:8081/_explorer/index.html"; Type = "HTTPS"},
    @{Name = "Service Bus Console"; Url = "http://localhost:8161"; Type = "HTTP"}
)

foreach ($service in $services) {
    Write-Host -NoNewline "Checking $($service.Name)... "
    try {
        $params = @{
            Uri = $service.Url
            Method = 'HEAD'
            TimeoutSec = 5
        }
        
        # Skip certificate validation for HTTPS endpoints
        if ($service.Type -eq "HTTPS") {
            $params.SkipCertificateCheck = $true
        }
        
        Invoke-WebRequest @params -ErrorAction SilentlyContinue | Out-Null
        Write-Host "Available" -ForegroundColor Green
    }
    catch {
        Write-Host "Not Available" -ForegroundColor Red
    }
}

# Step 6: Display environment information
Write-Host "`nEnvironment is ready!" -ForegroundColor Green
Write-Host "`nAvailable Endpoints:" -ForegroundColor Cyan
Write-Host "- Cosmos DB Explorer: https://localhost:8081/_explorer/index.html"
Write-Host "- Blob Storage: http://localhost:10000"
Write-Host "- Queue Storage: http://localhost:10001"
Write-Host "- Table Storage: http://localhost:10002"
Write-Host "- Service Bus Console: http://localhost:8161 (admin:admin)"

Write-Host "`nAvailable Databases and Containers:" -ForegroundColor Cyan
Write-Host "- ProductsDB"
Write-Host "  ├── Products (PartitionKey: /categoryId)"
Write-Host "  └── Categories (PartitionKey: /id)"
Write-Host "- CustomersDB"
Write-Host "  ├── Customers (PartitionKey: /country)"
Write-Host "  └── Orders (PartitionKey: /customerId)"
Write-Host "- AuctionsDB"
Write-Host "  ├── Auctions (PartitionKey: /id)"
Write-Host "  └── Lots (PartitionKey: /auctionId)"

Write-Host "`nService Bus Topics:" -ForegroundColor Cyan
Write-Host "- alp-reseeding (with default subscription)"

Write-Host "`nTo add or modify data:" -ForegroundColor Cyan
Write-Host "1. Edit JSON files in CosmosInitializer/DocumentFiles/*"
Write-Host "2. Run .\initialize-cosmosdb.bat to update the database"

Write-Host "`nTo shut down the environment:" -ForegroundColor Cyan
Write-Host "docker-compose down" -ForegroundColor DarkGray

Write-Host "`nSetup complete!" -ForegroundColor Green