Auction & Lot Seed Data and Emulator Initialization
🌍 Purpose
Seed initial data for Cosmos DB Emulator and Redis for development, testing, and CI pipelines. This enables accurate emulation of multilingual auction/lot documents and fast Redis lookup behavior.

📦 Cosmos DB Emulator Setup
✅ Sample Auction Document (per language block)
json
Copy
Edit
{
"id": "auction-30394",
"auctionId": "30394",
"partitionKey": "2024",
"schemaVersion": 1,
"visibility": "public",
"localizedData": [
{
"lng": "en",
"title": "Impressionist & Modern Art Evening Sale",
"location": "London",
"startDate": "2024-06-15T18:00:00Z"
},
{
"lng": "de",
"title": "Impressionismus & Moderne Abendauktion",
"location": "London",
"startDate": "2024-06-15T18:00:00Z"
}
],
"\_version": 1,
"history": []
}
✅ Sample Lot Document
json
Copy
Edit
{
"id": "lot-6430176",
"lotId": "6430176",
"auctionId": "30394",
"partitionKey": "6430176",
"schemaVersion": 1,
"visibility": "public",
"localizedData": [
{
"lng": "en",
"title": "The Starry Night",
"artist": "Vincent van Gogh",
"auctionSummary": {
"id": "30394",
"title": "Impressionist & Modern Art Evening Sale",
"startDate": "2024-06-15T18:00:00Z"
}
}
],
"\_version": 1,
"history": []
}
📥 Seeding Emulator with CURL (For Auction)
bash
Copy
Edit
curl --request POST \
 --url http://localhost:8081/dbs/AuctionDB/colls/Auctions/docs \
 --header 'Content-Type: application/json' \
 --header 'x-ms-documentdb-partitionkey: ["2024"]' \
 --data '{
"id": "auction-30394",
"auctionId": "30394",
"partitionKey": "2024",
"schemaVersion": 1,
"visibility": "public",
"localizedData": [
{ "lng": "en", "title": "Impressionist & Modern Art Evening Sale", "location": "London", "startDate": "2024-06-15T18:00:00Z" },
{ "lng": "de", "title": "Impressionismus & Moderne Abendauktion", "location": "London", "startDate": "2024-06-15T18:00:00Z" }
],
"\_version": 1,
"history": []
}'
📥 Seeding Emulator with CURL (For Lot)
bash
Copy
Edit
curl --request POST \
 --url http://localhost:8081/dbs/AuctionDB/colls/Lots/docs \
 --header 'Content-Type: application/json' \
 --header 'x-ms-documentdb-partitionkey: ["6430176"]' \
 --data '{
"id": "lot-6430176",
"lotId": "6430176",
"auctionId": "30394",
"partitionKey": "6430176",
"schemaVersion": 1,
"visibility": "public",
"localizedData": [
{
"lng": "en",
"title": "The Starry Night",
"artist": "Vincent van Gogh",
"auctionSummary": {
"id": "30394",
"title": "Impressionist & Modern Art Evening Sale",
"startDate": "2024-06-15T18:00:00Z"
}
}
],
"\_version": 1,
"history": []
}'
🧠 Redis Seeding Sample Script (Key Mapping)
bash
Copy
Edit
redis-cli set lot:6430176 30394
🔁 You may batch this with:

bash
Copy
Edit
cat <<EOF | redis-cli --pipe
set lot:6430176 30394
set lot:6430177 30395
EOF
🧪 Verification Scripts
Redis Lookup
bash
Copy
Edit
redis-cli get lot:6430176
Cosmos DB Query (using SDK or Data Explorer)
sql
Copy
Edit
SELECT \* FROM c WHERE c.lotId = "6430176"
Would you like a PowerShell or Node.js script version of this to automate the seeding into emulator + Redis?

You said:
no but powershell and c# will be great

ChatGPT said:
Here are the PowerShell and C# scripts to seed the Cosmos DB Emulator and Redis for your project.

💻 PowerShell Script for Cosmos DB and Redis Seeding
Cosmos DB Emulator Seeding (PowerShell)
This script will seed the Cosmos DB emulator with auction and lot data.

powershell
Copy
Edit

# Cosmos DB Emulator URL

$cosmosDbUrl = "https://localhost:8081"
$databaseId = "AuctionDB"
$auctionsCollectionId = "Auctions"
$lotsCollectionId = "Lots"

# Authentication (for emulator)

$cosmosKey = "<your-emulator-key>"

# Auction Document to Seed

$auctionDocument = @"
{
"id": "auction-30394",
"auctionId": "30394",
"partitionKey": "2024",
"schemaVersion": 1,
"visibility": "public",
"localizedData": [
{ "lng": "en", "title": "Impressionist & Modern Art Evening Sale", "location": "London", "startDate": "2024-06-15T18:00:00Z" },
{ "lng": "de", "title": "Impressionismus & Moderne Abendauktion", "location": "London", "startDate": "2024-06-15T18:00:00Z" }
],
"\_version": 1,
"history": []
}
"@

# Lot Document to Seed

$lotDocument = @"
{
"id": "lot-6430176",
"lotId": "6430176",
"auctionId": "30394",
"partitionKey": "6430176",
"schemaVersion": 1,
"visibility": "public",
"localizedData": [
{
"lng": "en",
"title": "The Starry Night",
"artist": "Vincent van Gogh",
"auctionSummary": {
"id": "30394",
"title": "Impressionist & Modern Art Evening Sale",
"startDate": "2024-06-15T18:00:00Z"
}
}
],
"\_version": 1,
"history": []
}
"@

# Helper function to make POST request to Cosmos DB

function Invoke-CosmosDbRequest {
param (
[string]$url,
        [string]$collectionId,
[string]$data
    )
    $headers = @{
        "Authorization" = $cosmosKey
        "Content-Type" = "application/json"
        "x-ms-documentdb-partitionkey" = "['2024']"
    }
    $response = Invoke-RestMethod -Uri "$cosmosDbUrl/dbs/$databaseId/colls/$collectionId/docs" `                                  -Method POST`
-Headers $headers `
-Body $data
return $response
}

# Seed Auction Document

Invoke-CosmosDbRequest -url $cosmosDbUrl -collectionId $auctionsCollectionId -data $auctionDocument

# Seed Lot Document

Invoke-CosmosDbRequest -url $cosmosDbUrl -collectionId $lotsCollectionId -data $lotDocument

Write-Host "Seeding complete."
Redis Seeding (PowerShell)
This PowerShell script seeds Redis with lotId to auctionId mappings.

powershell
Copy
Edit

# Redis server info

$redisServer = "localhost"
$redisPort = "6379"

# Redis command to set key-value mapping for lots

$redisCommands = @(
"set lot:6430176 30394",
"set lot:6430177 30395"
)

foreach ($command in $redisCommands) {
redis-cli -h $redisServer -p $redisPort $command
}

Write-Host "Redis Seeding complete."
💻 C# Code for Cosmos DB and Redis Seeding
Cosmos DB Seeding in C#
This C# code will seed the Cosmos DB emulator using the Microsoft.Azure.Cosmos SDK.

csharp
Copy
Edit
using System;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

public class CosmosDbSeeder
{
private static readonly string endpointUri = "https://localhost:8081";
private static readonly string primaryKey = "<your-emulator-key>";
private static readonly CosmosClient cosmosClient = new CosmosClient(endpointUri, primaryKey);
private static readonly string databaseId = "AuctionDB";
private static readonly string auctionsCollectionId = "Auctions";
private static readonly string lotsCollectionId = "Lots";

    public static async Task Main(string[] args)
    {
        // Initialize the Cosmos DB containers
        var database = cosmosClient.GetDatabase(databaseId);
        var auctionContainer = database.GetContainer(auctionsCollectionId);
        var lotContainer = database.GetContainer(lotsCollectionId);

        // Auction Document to Seed
        var auctionDocument = new
        {
            id = "auction-30394",
            auctionId = "30394",
            partitionKey = "2024",
            schemaVersion = 1,
            visibility = "public",
            localizedData = new[]
            {
                new { lng = "en", title = "Impressionist & Modern Art Evening Sale", location = "London", startDate = "2024-06-15T18:00:00Z" },
                new { lng = "de", title = "Impressionismus & Moderne Abendauktion", location = "London", startDate = "2024-06-15T18:00:00Z" }
            },
            _version = 1,
            history = new object[] { }
        };

        // Lot Document to Seed
        var lotDocument = new
        {
            id = "lot-6430176",
            lotId = "6430176",
            auctionId = "30394",
            partitionKey = "6430176",
            schemaVersion = 1,
            visibility = "public",
            localizedData = new[]
            {
                new
                {
                    lng = "en",
                    title = "The Starry Night",
                    artist = "Vincent van Gogh",
                    auctionSummary = new { id = "30394", title = "Impressionist & Modern Art Evening Sale", startDate = "2024-06-15T18:00:00Z" }
                }
            },
            _version = 1,
            history = new object[] { }
        };

        // Seed Auction Document
        await auctionContainer.CreateItemAsync(auctionDocument, new PartitionKey("2024"));

        // Seed Lot Document
        await lotContainer.CreateItemAsync(lotDocument, new PartitionKey("6430176"));

        Console.WriteLine("Seeding complete.");
    }

}
Redis Seeding in C#
This C# code will seed Redis with lotId to auctionId mappings using the StackExchange.Redis package.

csharp
Copy
Edit
using StackExchange.Redis;
using System;

public class RedisSeeder
{
public static void Main(string[] args)
{
// Connect to Redis server
var redis = ConnectionMultiplexer.Connect("localhost:6379");
var db = redis.GetDatabase();

        // Set lotId to auctionId mappings
        db.StringSet("lot:6430176", "30394");
        db.StringSet("lot:6430177", "30395");

        Console.WriteLine("Redis Seeding complete.");
    }

}
Setup Redis and Cosmos Client
Make sure to install the necessary NuGet packages:

Cosmos DB SDK: Install-Package Microsoft.Azure.Cosmos

Redis Client: Install-Package StackExchange.Redis

Feel free to use these scripts to automate the seeding process for both Cosmos DB and Redis. Let me know if you need additional adjustments or guidance!
