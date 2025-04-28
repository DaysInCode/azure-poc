# This script initializes Cosmos DB databases, containers, and sample documents

# Ensure the CosmosDB certificate is installed
if (-not (Test-Path Cert:\LocalMachine\Root\*) -or -not (Get-ChildItem Cert:\LocalMachine\Root | Where-Object { $_.Subject -like "*CN=localhost*" })) {
    Write-Host "Cosmos DB certificate not found. Installing..."
    .\setup-certificates.ps1
}

# Install required NuGet packages if needed
if (-not (Get-Package Microsoft.Azure.Cosmos -ErrorAction SilentlyContinue)) {
    Install-Package Microsoft.Azure.Cosmos -Scope CurrentUser -Force
}

# Import the Cosmos SDK
Add-Type -Path "$env:USERPROFILE\.nuget\packages\microsoft.azure.cosmos\*\lib\netstandard2.0\Microsoft.Azure.Cosmos.dll"

# Configuration 
$endpoint = "https://localhost:8081"
$masterKey = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="
$databaseConfigs = @(
    @{
        Name = "ProductsDB"
        Containers = @(
            @{
                Name = "Products"
                PartitionKeyPath = "/categoryId"
                Documents = @(
                    @{
                        id = "p1"
                        name = "Laptop"
                        price = 999.99
                        categoryId = "electronics"
                        description = "High-performance laptop"
                    },
                    @{
                        id = "p2"
                        name = "Smartphone"
                        price = 699.99
                        categoryId = "electronics"
                        description = "Latest smartphone model"
                    }
                )
            },
            @{
                Name = "Categories"
                PartitionKeyPath = "/id"
                Documents = @(
                    @{
                        id = "electronics"
                        name = "Electronics"
                        description = "Electronic devices and gadgets"
                    },
                    @{
                        id = "homegoods"
                        name = "Home Goods"
                        description = "Items for your home"
                    }
                )
            }
        )
    },
    @{
        Name = "CustomersDB"
        Containers = @(
            @{
                Name = "Customers"
                PartitionKeyPath = "/country"
                Documents = @(
                    @{
                        id = "c1"
                        name = "John Smith"
                        email = "john@example.com"
                        country = "USA"
                        orders = @(
                            @{
                                id = "o1"
                                date = "2025-04-01"
                                total = 1299.99
                            }
                        )
                    },
                    @{
                        id = "c2"
                        name = "Jane Doe"
                        email = "jane@example.com"
                        country = "Canada"
                        orders = @(
                            @{
                                id = "o2"
                                date = "2025-04-10"
                                total = 849.50
                            }
                        )
                    }
                )
            },
            @{
                Name = "Orders"
                PartitionKeyPath = "/customerId"
                Documents = @(
                    @{
                        id = "order1"
                        customerId = "c1"
                        items = @(
                            @{
                                productId = "p1"
                                quantity = 1
                                price = 999.99
                            },
                            @{
                                productId = "p2"
                                quantity = 1
                                price = 699.99
                            }
                        )
                        total = 1699.98
                        status = "shipped"
                        date = "2025-04-15"
                    }
                )
            }
        )
    }
)

try {
    # Create a CosmosClient instance with TLS/SSL certificate validation disabled for local development
    $clientOptions = New-Object Microsoft.Azure.Cosmos.CosmosClientOptions
    $clientOptions.ConnectionMode = [Microsoft.Azure.Cosmos.ConnectionMode]::Gateway
    $clientOptions.HttpClientFactory = {
        $handler = New-Object System.Net.Http.HttpClientHandler
        $handler.ServerCertificateCustomValidationCallback = [System.Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }
        return New-Object System.Net.Http.HttpClient($handler)
    }

    $client = New-Object Microsoft.Azure.Cosmos.CosmosClient($endpoint, $masterKey, $clientOptions)
    
    # Process each database configuration
    foreach ($dbConfig in $databaseConfigs) {
        Write-Host "Creating/Ensuring database: $($dbConfig.Name)" -ForegroundColor Green
        $database = $client.CreateDatabaseIfNotExistsAsync($dbConfig.Name).GetAwaiter().GetResult()
        
        # Process each container in the database
        foreach ($containerConfig in $dbConfig.Containers) {
            Write-Host "  Creating/Ensuring container: $($containerConfig.Name)" -ForegroundColor Cyan
            
            # Set container properties
            $containerProperties = New-Object Microsoft.Azure.Cosmos.ContainerProperties
            $containerProperties.Id = $containerConfig.Name
            $containerProperties.PartitionKeyPath = $containerConfig.PartitionKeyPath
            
            $container = $database.Database.CreateContainerIfNotExistsAsync($containerProperties, 400).GetAwaiter().GetResult()
            
            # Add documents to the container
            foreach ($doc in $containerConfig.Documents) {
                Write-Host "    Adding document: $($doc.id)" -ForegroundColor Yellow
                $docJson = $doc | ConvertTo-Json -Depth 10
                $container.Container.UpsertItemAsync($docJson).GetAwaiter().GetResult() | Out-Null
            }
        }
    }
    
    Write-Host "Cosmos DB initialization completed successfully!" -ForegroundColor Green
} 
catch {
    Write-Host "Error initializing Cosmos DB: $_" -ForegroundColor Red
}
finally {
    # Dispose the client when done
    if ($client) {
        $client.Dispose()
    }
}