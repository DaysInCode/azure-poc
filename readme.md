# Local Environment Endpoints and Usage Guide

This document provides a comprehensive guide to the development services available in this Azure POC environment.

## Endpoints Overview

### Azurite (Azure Storage Emulator)

- Blob Storage: http://localhost:10000
- Queue Storage: http://localhost:10001
- Table Storage: http://localhost:10002

### Cosmos DB Emulator

- Endpoint: https://localhost:8081
- Management Portal: https://localhost:8081/_explorer/index.html
- Additional ports: 8900, 8901, 8979, 10250-10255 (for internal operations)

### Service Bus (ActiveMQ Artemis)

- AMQP Endpoint: localhost:5672
- Web Console: http://localhost:8161
  - Username: admin
  - Password: admin
- Default Topic: alp-reseeding

## Step-by-Step Usage Guide

### 1. Starting the Environment

```powershell
# Start all services
docker-compose up -d
```

### 2. Setting up Cosmos DB

Wait approximately 90 seconds for Cosmos DB to initialize, then:

```powershell
# Download and install the Cosmos DB certificate
.\setup-certificates.ps1
```

### 3. Initializing Cosmos DB with Sample Data

The environment includes a file-based Cosmos DB initializer that populates databases and containers using individual JSON document files. This approach makes it easy for developers and testers to add, modify, or update sample data.

To run the initializer:

```powershell
# Build and run the C# Cosmos DB initializer
.\initialize-cosmosdb.bat
```

#### Adding or Modifying Sample Data

You can easily modify existing data or add new documents:

1. Navigate to the corresponding folder under `CosmosInitializer/DocumentFiles/`
2. Edit existing JSON files or add new ones following the established naming pattern
3. Re-run the initializer to update the Cosmos DB with your changes

The directory structure follows this pattern:
```
DocumentFiles/
├── ProductsDB/
│   ├── Products/
│   │   ├── laptop.json
│   │   └── smartphone.json
│   └── Categories/
│       ├── electronics.json
│       └── homegoods.json
├── CustomersDB/
│   ├── Customers/
│   │   ├── john-smith.json
│   │   └── jane-doe.json
│   └── Orders/
│       └── order1.json
└── AuctionsDB/
    ├── Auctions/
    │   └── auction-30394.json
    └── Lots/
        └── lot-6430176.json
```

#### Database Configuration

The database structure (databases, containers, and partition keys) is defined in `CosmosInitializer/appsettings.json`. If you need to add a new database or container:

1. Add the configuration to `appsettings.json`
2. Create the corresponding folder structure under `DocumentFiles/`
3. Add JSON document files to the new folder
4. Run the initializer

#### Available Databases and Containers

The environment is preconfigured with the following databases and containers:

##### ProductsDB
- **Products** - Partition Key: `/categoryId`
- **Categories** - Partition Key: `/id`

##### CustomersDB
- **Customers** - Partition Key: `/country`
- **Orders** - Partition Key: `/customerId`

##### AuctionsDB
- **Auctions** - Partition Key: `/id`
- **Lots** - Partition Key: `/auctionId`

#### Service Bus Topics

The initializer also creates a default Service Bus topic named `alp-reseeding` with a default subscription. You can add more topics by updating the `ServiceBus.Topics` array in `appsettings.json`.

### 4. Connecting to Azurite

```csharp
// Connection string for Azure Blob Storage
string blobConnectionString = "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://localhost:10000/devstoreaccount1;";

// Connection string for Azure Queue Storage
string queueConnectionString = "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;QueueEndpoint=http://localhost:10001/devstoreaccount1;";

// Connection string for Azure Table Storage
string tableConnectionString = "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;TableEndpoint=http://localhost:10002/devstoreaccount1;";
```

### 5. Connecting to Cosmos DB

```csharp
// Cosmos DB connection string
string cosmosConnectionString = "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==;";

// Create a client
using Microsoft.Azure.Cosmos.CosmosClient client = new(cosmosConnectionString, new CosmosClientOptions
{
    ConnectionMode = ConnectionMode.Gateway,
    HttpClientFactory = () => new HttpClient(new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    })
});
```

### 6. Connecting to Service Bus (ActiveMQ Artemis)

```csharp
// Using AMQP protocol
string serviceBusConnectionString = "Endpoint=sb://localhost:5672/;SharedAccessKeyName=admin;SharedAccessKey=admin";

// Using Azure.Messaging.ServiceBus
var client = new ServiceBusClient(serviceBusConnectionString);
var sender = client.CreateSender("alp-reseeding");
```

### 7. Web Access to Management Consoles

- **Cosmos DB Explorer:** https://localhost:8081/_explorer/index.html
- **ActiveMQ Artemis Console:** http://localhost:8161
  - Login with username: `admin` and password: `admin`

### 8. Shutting Down

```powershell
# Stop all services
docker-compose down

# To stop and remove volumes
docker-compose down -v
```

This environment provides local emulation for Azure Blob/Queue/Table Storage, Cosmos DB, and Service Bus functionalities for development and testing.
