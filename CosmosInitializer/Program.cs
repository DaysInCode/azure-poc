using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using CosmosInitializer.Models;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;

namespace CosmosInitializer
{
    class Program
    {
        private static readonly string DocumentFilesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DocumentFiles");
        private static readonly HttpClientHandler HttpClientHandler = new()
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        private static Dictionary<string, string> _containerPartitionKeys = new Dictionary<string, string>();

        static async Task Main(string[] args)
        {
            try
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Starting Cosmos DB and Service Bus Initializer...");
                Console.WriteLine("========================================");
                Console.ResetColor();

                // Load configuration
                var config = LoadConfiguration();
                
                // Create Cosmos client with SSL validation disabled for local emulator
                var clientOptions = new CosmosClientOptions
                {
                    ConnectionMode = ConnectionMode.Gateway,
                    HttpClientFactory = () => new HttpClient(HttpClientHandler)
                };
                
                using var client = new CosmosClient(config.Endpoint, config.Key, clientOptions);
                
                // Build the container partition key lookup dictionary
                BuildContainerPartitionKeyLookup(config);
                
                // Process each database and container
                foreach (var dbConfig in config.Databases)
                {
                    await InitializeDatabaseAsync(client, dbConfig);
                }

                // Initialize Service Bus topics if configured
                await InitializeServiceBusTopicsAsync(config.ServiceBus);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\nInitialization completed successfully!");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error during initialization: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                Console.ResetColor();
                Environment.Exit(1);
            }
        }

        private static void BuildContainerPartitionKeyLookup(CosmosDbConfig config)
        {
            foreach (var dbConfig in config.Databases)
            {
                foreach (var containerConfig in dbConfig.Containers)
                {
                    // Store the partition key path for each container
                    // Remove the leading "/" from the path for easier lookup
                    string partitionKeyProperty = containerConfig.PartitionKeyPath.TrimStart('/');
                    _containerPartitionKeys[containerConfig.Name] = partitionKeyProperty;
                }
            }
        }

        private static async Task InitializeServiceBusTopicsAsync(ServiceBusConfig? serviceBusConfig)
        {
            if (serviceBusConfig?.Topics == null || !serviceBusConfig.Topics.Any())
                return;

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\nInitializing Service Bus Topics...");
            Console.ResetColor();

            try
            {
                // Connect to the local Service Bus emulator (ActiveMQ Artemis)
                // For local development with ActiveMQ Artemis, use AMQP protocol
                var connectionString = "Endpoint=sb://localhost:5672/;SharedAccessKeyName=admin;SharedAccessKey=admin";
                var adminClient = new ServiceBusAdministrationClient(connectionString);

                foreach (var topicName in serviceBusConfig.Topics)
                {
                    try
                    {
                        Console.Write($"  Creating topic: {topicName}... ");

                        // Create topic options with default settings
                        var options = new CreateTopicOptions(topicName)
                        {
                            DefaultMessageTimeToLive = TimeSpan.FromDays(14),
                            MaxSizeInMegabytes = 1024,
                            EnableBatchedOperations = true
                        };

                        // Check if topic exists first to avoid exception
                        var topicExists = await adminClient.TopicExistsAsync(topicName);
                        if (!topicExists)
                        {
                            await adminClient.CreateTopicAsync(options);
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("Created");
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("Already exists");
                        }
                        Console.ResetColor();

                        // Create a default subscription for the topic
                        var defaultSubName = $"{topicName}-subscription";
                        Console.Write($"    Creating subscription: {defaultSubName}... ");
                        
                        var subExists = await adminClient.SubscriptionExistsAsync(topicName, defaultSubName);
                        if (!subExists)
                        {
                            var subOptions = new CreateSubscriptionOptions(topicName, defaultSubName)
                            {
                                DefaultMessageTimeToLive = TimeSpan.FromDays(14),
                                EnableBatchedOperations = true
                            };
                            
                            await adminClient.CreateSubscriptionAsync(subOptions);
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("Created");
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("Already exists");
                        }
                        Console.ResetColor();
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Failed to create topic {topicName}: {ex.Message}");
                        Console.ResetColor();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Service Bus initialization failed: {ex.Message}");
                Console.WriteLine($"Make sure the Service Bus emulator (ActiveMQ Artemis) is running.");
                Console.ResetColor();
            }
        }

        private static CosmosDbConfig LoadConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false);

            var configuration = builder.Build();
            var cosmosConfig = configuration.GetSection("CosmosDb").Get<CosmosDbConfig>();

            if (cosmosConfig == null)
            {
                throw new InvalidOperationException("Failed to load CosmosDb configuration from appsettings.json");
            }

            // Load Service Bus configuration if available
            cosmosConfig.ServiceBus = configuration.GetSection("ServiceBus").Get<ServiceBusConfig>();

            return cosmosConfig;
        }

        private static async Task InitializeDatabaseAsync(CosmosClient client, DatabaseConfig dbConfig)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\nCreating/Ensuring database: {dbConfig.Name}");
            Console.ResetColor();

            DatabaseResponse dbResponse = await client.CreateDatabaseIfNotExistsAsync(dbConfig.Name);
            Database database = dbResponse.Database;
            
            foreach (var containerConfig in dbConfig.Containers)
            {
                await InitializeContainerAsync(database, containerConfig, dbConfig.Name);
            }
        }

        private static async Task InitializeContainerAsync(Database database, ContainerConfig containerConfig, string dbName)
        {
            Console.WriteLine($"  Creating/Ensuring container: {containerConfig.Name}");
            
            ContainerProperties properties = new()
            {
                Id = containerConfig.Name,
                PartitionKeyPath = containerConfig.PartitionKeyPath
            };
            
            ContainerResponse containerResponse = await database.CreateContainerIfNotExistsAsync(properties, throughput: 400);
            Container container = containerResponse.Container;
            
            // Directory path for documents specific to this database and container
            string documentsPath = Path.Combine(DocumentFilesPath, dbName, containerConfig.Name);
            
            // Check if the directory exists before attempting to read documents
            if (Directory.Exists(documentsPath))
            {
                await ImportDocumentsAsync(container, documentsPath);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"    Warning: Document directory not found: {documentsPath}");
                Console.ResetColor();
            }
        }

        private static async Task ImportDocumentsAsync(Container container, string documentsPath)
        {
            int documentCount = 0;
            
            // Get all .json files in the directory
            var documentFiles = Directory.GetFiles(documentsPath, "*.json");
            
            foreach (var file in documentFiles)
            {
                try
                {
                    string fileName = Path.GetFileName(file);
                    Console.Write($"    Importing document: {fileName}... ");
                    
                    // Read and parse the JSON file
                    string jsonContent = await File.ReadAllTextAsync(file);
                    using JsonDocument jsonDocument = JsonDocument.Parse(jsonContent);

                    // Extract the id from the document for logging purposes
                    JsonElement root = jsonDocument.RootElement;
                    string? documentId = root.TryGetProperty("id", out JsonElement idElement) ? idElement.GetString() : "unknown";
                    
                    // Upsert the document to Cosmos DB
                    await container.UpsertItemAsync<object>(
                        JsonSerializer.Deserialize<object>(jsonContent)!, 
                        new PartitionKey(GetPartitionKeyValue(root, container.Id))
                    );
                    
                    documentCount++;
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Success (id: {documentId})");
                    Console.ResetColor();
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Failed: {ex.Message}");
                    Console.ResetColor();
                }
            }
            
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"    Total documents imported: {documentCount}");
            Console.ResetColor();
        }

        private static string GetPartitionKeyValue(JsonElement document, string containerId)
        {
            // Get the partition key property from our lookup dictionary
            if (!_containerPartitionKeys.TryGetValue(containerId, out string partitionKeyProperty))
            {
                // If not found in dictionary, default to id
                partitionKeyProperty = "id";
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"    Warning: No partition key defined for container {containerId}, using 'id' as default");
                Console.ResetColor();
            }
            
            // Extract the partition key value from the document
            if (document.TryGetProperty(partitionKeyProperty, out JsonElement pkElement) && pkElement.ValueKind == JsonValueKind.String)
            {
                return pkElement.GetString()!;
            }
            
            // If we can't find the partition key, throw an exception
            throw new InvalidOperationException($"Could not find partition key property '{partitionKeyProperty}' in document");
        }
    }
}
