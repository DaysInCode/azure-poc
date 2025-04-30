/**
 * Cosmos DB Initialization Script
 * This script creates databases, containers, and populates them with sample documents
 */

const { CosmosClient } = require("@azure/cosmos");

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

// Database configurations with containers and sample documents
const databaseConfigs = [];

// Create a new client
const client = new CosmosClient(clientOptions);

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
  } catch (error) {
    console.error("Error initializing Cosmos DB:", error);
  }
}

// Run the initialization
initializeCosmosDB();
