/**
 * Cosmos DB Initialization Script
 * This script creates databases, containers, and populates them with sample documents
 */

const { CosmosClient } = require('@azure/cosmos');

// Configuration
const endpoint = 'https://localhost:8081';
const key = 'C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==';
const clientOptions = {
  endpoint,
  key,
  connectionPolicy: {
    enableEndpointDiscovery: false,
  },
  // Disable SSL validation for local emulator
  agent: new (require('https')).Agent({ rejectUnauthorized: false })
};

// Database configurations with containers and sample documents
const databaseConfigs = [
  {
    name: 'ProductsDB',
    containers: [
      {
        name: 'Products',
        partitionKey: '/categoryId',
        documents: [
          {
            id: 'p1',
            name: 'Laptop',
            price: 999.99,
            categoryId: 'electronics',
            description: 'High-performance laptop'
          },
          {
            id: 'p2',
            name: 'Smartphone',
            price: 699.99,
            categoryId: 'electronics',
            description: 'Latest smartphone model'
          }
        ]
      },
      {
        name: 'Categories',
        partitionKey: '/id',
        documents: [
          {
            id: 'electronics',
            name: 'Electronics',
            description: 'Electronic devices and gadgets'
          },
          {
            id: 'homegoods',
            name: 'Home Goods',
            description: 'Items for your home'
          }
        ]
      }
    ]
  },
  {
    name: 'CustomersDB',
    containers: [
      {
        name: 'Customers',
        partitionKey: '/country',
        documents: [
          {
            id: 'c1',
            name: 'John Smith',
            email: 'john@example.com',
            country: 'USA',
            orders: [
              {
                id: 'o1',
                date: '2025-04-01',
                total: 1299.99
              }
            ]
          },
          {
            id: 'c2',
            name: 'Jane Doe',
            email: 'jane@example.com',
            country: 'Canada',
            orders: [
              {
                id: 'o2',
                date: '2025-04-10',
                total: 849.50
              }
            ]
          }
        ]
      },
      {
        name: 'Orders',
        partitionKey: '/customerId',
        documents: [
          {
            id: 'order1',
            customerId: 'c1',
            items: [
              {
                productId: 'p1',
                quantity: 1,
                price: 999.99
              },
              {
                productId: 'p2',
                quantity: 1,
                price: 699.99
              }
            ],
            total: 1699.98,
            status: 'shipped',
            date: '2025-04-15'
          }
        ]
      }
    ]
  }
];

// Create a new client
const client = new CosmosClient(clientOptions);

async function initializeCosmosDB() {
  try {
    console.log('Starting Cosmos DB initialization...');

    // Process each database
    for (const dbConfig of databaseConfigs) {
      console.log(`Creating/ensuring database: ${dbConfig.name}`);
      const { database } = await client.databases.createIfNotExists({
        id: dbConfig.name
      });

      // Process each container in the database
      for (const containerConfig of dbConfig.containers) {
        console.log(`  Creating/ensuring container: ${containerConfig.name}`);
        const { container } = await database.containers.createIfNotExists({
          id: containerConfig.name,
          partitionKey: containerConfig.partitionKey
        });

        // Add documents to the container
        for (const document of containerConfig.documents) {
          console.log(`    Adding document: ${document.id}`);
          await container.items.upsert(document);
        }
      }
    }

    console.log('Cosmos DB initialization completed successfully!');
  } catch (error) {
    console.error('Error initializing Cosmos DB:', error);
  }
}

// Run the initialization
initializeCosmosDB();