# 📂 Project Document: Cosmos DB Auction & Lot System

## 📌 Overview

This document outlines the architecture, Cosmos DB modeling, sample data, and query operations for a high-volume auction system with multilingual support. It is optimized for read-heavy workloads, uses denormalized data colocated in the Lots container, and supports full versioning and language-specific updates.

## ☁️ Technologies Used

- Azure Cosmos DB (NoSQL, Emulator Compatible)
- Azure Functions (.NET C#)
- Azure Cosmos DB Change Feed Processor
- Redis (as a lookup cache)
- Language-specific JSON structure per document
- Dockerized Redis and Function host

## 📃 Cosmos DB Container Design

### Auctions Container

- **Container Name:** `Auctions`
- **Partition Key:** `/year`
- **ID:** `auctionId`

```json
{
  "id": "30394",
  "year": 2024,
  "startDate": "2024-06-01T00:00:00Z",
  "status": "Upcoming",
  "localizedData": [
    {
      "lng": "en",
      "title": "Impressionist Sale",
      "location": "London"
    },
    {
      "lng": "de",
      "title": "Impressionisten-Verkauf",
      "location": "London"
    }
  ],
  "_version": 3,
  "history": [],
  "schemaVersion": "v1",
  "visibility": "internal" // can be "public" or "internal"
}
```

---

### Lots Container

- **Container Name:** `Lots`
- **Partition Key:** `/auctionId`
- **ID:** `lotId`
- **Note:** Lots can be reassigned between auctions or be temporarily unassigned.

```json
{
  "id": "6430176",
  "lotId": "6430176",
  "auctionId": "30394", // Can be null if unassigned
  "status": "Available",
  "localizedData": [
    {
      "lng": "en",
      "title": "Water Lilies",
      "artist": "Claude Monet",
      "auctionSummary": {
        "id": "30394",
        "title": "Impressionist Sale",
        "startDate": "2024-06-01T00:00:00Z"
      }
    },
    {
      "lng": "de",
      "title": "Seerosen",
      "artist": "Claude Monet",
      "auctionSummary": {
        "id": "30394",
        "title": "Impressionisten-Verkauf",
        "startDate": "2024-06-01T00:00:00Z"
      }
    }
  ],
  "_version": 4,
  "history": [],
  "schemaVersion": "v1",
  "visibility": "internal" // can be "public" or "internal"
}
```

---

### Lot Redis Lookup Strategy

- **Key:** `lot:{lotId}`
- **Value:** `{ "auctionId": "30394" }`
- **Source:** Cosmos DB Change Feed processor
- **Persistence:** Redis entries are not expired automatically
- **Use Case:** Fast lookup of partition key to avoid cross-partition Cosmos DB queries
- **Fallback:** If Redis misses, perform Cosmos DB cross-partition query and backfill Redis

---

## 🔪 Sample CURL for Data Ingestion

### Auction Data

```bash
curl --location 'https://api-nonprod.christies.com/uat/auctions/30394' \
--header 'Accept: application/vnd.christies.v2+json' \
--header 'Accept-Language: en'
```

### Lot Data

```bash
curl --location 'https://api-prod.christies.com/lots/6430176' \
--header 'Accept: application/vnd.christies.v3+json' \
--header 'Ocp-Apim-Subscription-Key: {{Ocp-Apim-Subscription-Key}}' \
--header 'countryCode: GB'
```

Use these CURL commands as templates to pre-populate your Cosmos DB Emulator.

---

## 🔎 Sample Cosmos DB Queries

### 1. Get Lot by `lotId` and Language (via Redis lookup of partition key)

```sql
SELECT * FROM c WHERE c.lotId = "6430176"
```

### 2. Get Auction by `auctionId` and Language

```sql
SELECT VALUE a
FROM a IN Auctions
WHERE a.id = "30394" AND ARRAY_CONTAINS(a.localizedData, {"lng": "en"}, true)
```

### 3. Update (replace) a localized block in Auction

```csharp
// Pseudo-code in C#
auction.localizedData = auction.localizedData.Where(l => l.lng != "en").ToList();
auction.localizedData.Add(newEnglishBlock);
await container.ReplaceItemAsync(auction, auction.id, new PartitionKey(auction.year));
```

### 4. Change Feed: On Auction Change, Update Lots

```csharp
foreach (var lot in GetLotsByAuctionId(updatedAuction.Id)) {
   var updatedLocalized = updatedAuction.localizedData.Find(x => x.lng == targetLang);
   var lotBlock = lot.localizedData.Find(x => x.lng == targetLang);
   lotBlock.auctionSummary = new { id = ..., title = updatedLocalized.title };
   await container.ReplaceItemAsync(lot, lot.id, new PartitionKey(lot.auctionId));
}
```

### 5. Change Feed to Redis Cache for Partition Lookup

```csharp
foreach (var lot in changes)
{
   await redis.StringSetAsync($"lot:{lot.lotId}", lot.auctionId);
}
```

---

## 📌 Optimization Notes

- **Indexing Policy:** Include only `lotId`, `auctionId`, `localizedData.lng`, `localizedData.title`
- **Query Tip:** Use projections (`SELECT VALUE l.localizedData`) to minimize RUs
- **Versioning:** Always update `_version`, append full previous doc to `history`
- **Change Feed:** Listen for changes by document, not individual fields
- **Redis:** Used to optimize cross-partition avoidance
- **Performance:** Ensure document load times stay <100ms from cache or Cosmos
- **Validation:** Include a document readiness validator for internal vs. public visibility
- **Schema Validation:** Enforced via schema version and unit test contract
- **Testing:** Full integration + unit test suite, including:
  - Redis fallback logic
  - Change feed triggers
  - Lot reassignment
  - Localization integrity

---

## 📘 References

- [Change Feed Patterns](https://docs.azure.cn/en-us/cosmos-db/nosql/change-feed-design-patterns?tabs=latest-version)
- [Cosmos DB Partition Key Best Practices](https://docs.azure.cn/en-us/cosmos-db/nosql/model-partition-example)
