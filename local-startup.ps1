# Start the containers
docker-compose up -d

# Wait for Cosmos DB to start (about 1-2 minutes)
Write-Host "Waiting for Cosmos DB emulator to start (90 seconds)..." -ForegroundColor Yellow
Start-Sleep -Seconds 90

# Setup the Cosmos DB certificate
Write-Host "Setting up Cosmos DB certificate..." -ForegroundColor Yellow
.\setup-certificates.ps1

# Initialize the Cosmos DB with databases, containers, and documents using file-based C# initializer
Write-Host "Initializing Cosmos DB with sample data from JSON files..." -ForegroundColor Yellow
.\initialize-cosmosdb.bat

Write-Host "Local environment is ready!" -ForegroundColor Green
Write-Host "You can access:
- Cosmos DB Explorer: https://localhost:8081/_explorer/index.html
- Blob Storage: http://localhost:10000
- Queue Storage: http://localhost:10001
- Table Storage: http://localhost:10002
- Service Bus Console: http://localhost:8161"