/**
 * Cosmos DB Initialization Script
 * This script creates databases, containers, and populates them with sample documents
 */

const { CosmosClient } = require("@azure/cosmos");
const redis = require("redis");

// Configuration
const endpoint = "https://localhost:8081";
const key =
  "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
const clientOptions = {
  endpoint,
  key,
  connectionPolicy: {
    enableEndpointDiscovery: false,
  },
  // Disable SSL validation for local emulator
  agent: new (require("https").Agent)({ rejectUnauthorized: false }),
};

// Redis client configuration
const redisClient = redis.createClient({ url: "redis://localhost:6379" });

// Database configurations with containers and sample documents
const databaseConfigs = [];

// Create a new client
const client = new CosmosClient(clientOptions);

async function initializeRedis() {
  try {
    await redisClient.connect();
    console.log("Connected to Redis.");

    // Seed Redis with data from Cosmos DB
    for (const dbConfig of databaseConfigs) {
      for (const containerConfig of dbConfig.containers) {
        if (containerConfig.name === "Lots") {
          const { container } = client.database(dbConfig.name).container(containerConfig.name);
          const { resources: lots } = await container.items.readAll().fetchAll();

          for (const lot of lots) {
            if (lot.lotId && lot.auctionId) {
              await redisClient.set(`lot:${lot.lotId}`, lot.auctionId);
              console.log(`Seeded Redis: lot:${lot.lotId} -> ${lot.auctionId}`);
            }
          }
        }
      }
    }
  } catch (error) {
    console.error("Error initializing Redis:", error);
  } finally {
    await redisClient.disconnect();
  }
}

async function initializeCosmosDB() {
  try {
    console.log("Starting Cosmos DB initialization...");

    // Process each database
    for (const dbConfig of databaseConfigs) {
      console.log(`Creating/ensuring database: ${dbConfig.name}`);
      const { database } = await client.databases.createIfNotExists({
        id: dbConfig.name,
      });

      // Process each container in the database
      for (const containerConfig of dbConfig.containers) {
        console.log(`  Creating/ensuring container: ${containerConfig.name}`);
        const { container } = await database.containers.createIfNotExists({
          id: containerConfig.name,
          partitionKey: containerConfig.partitionKey,
        });

        // Add documents to the container
        for (const document of containerConfig.documents) {
          console.log(`    Adding document: ${document.id}`);
          await container.items.upsert(document);
        }
      }
    }

    console.log("Cosmos DB initialization completed successfully!");

    // Initialize Redis after Cosmos DB
    await initializeRedis();
  } catch (error) {
    console.error("Error initializing Cosmos DB:", error);
  }
}

// Run the initialization
initializeCosmosDB();
