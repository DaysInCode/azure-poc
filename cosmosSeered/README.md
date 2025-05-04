# cosmosSeered Project

This project provides utilities for seeding data into Azure Cosmos DB and Azure Service Bus (using the Azure Service Bus Emulator for local development).

## Features

- **CosmosDbInserter.cs**: Populates Cosmos DB with sample data from the `AzureCosmosData` folder.
- **ServiceBusSeeder.cs**: Pushes messages to Azure Service Bus queues and topics using data from the `ServiceBusData` folder.

## Usage

### 1. Seeding Cosmos DB

- Place your sample data JSON files in the appropriate subfolders under `AzureCosmosData/` (e.g., `Auctions/`, `Lots/`).
- Run the CosmosDbInserter utility to insert this data into your local Cosmos DB emulator or a configured Cosmos DB instance.

### 2. Seeding Azure Service Bus

- Place your message JSON files in the `ServiceBusData/queue/` or `ServiceBusData/topic/` folders.
- Run the ServiceBusSeeder utility to send these messages to the configured Azure Service Bus (or emulator).
- The connection string for the Service Bus Emulator can be set in `ServiceBusSeeder.cs` (update as needed for your environment).

## Configuration

- Ensure your local Cosmos DB emulator and Azure Service Bus emulator are running (see the root `docker-compose.yml` for setup).
- Update connection strings in the code if your emulator is running on a different host or with different credentials.

## Example (Using Docker)

To seed both Cosmos DB and Service Bus using containers:

1. Start the emulators:
   ```sh
   docker-compose up
   ```
2. Build and run the project (from the `cosmosSeered` directory):
   ```sh
   dotnet run -- --targetType cosmos --path ./AzureCosmosData
   dotnet run -- --targetType servicebus --path ./ServiceBusData
   ```

## Example (Using Compiled Executable)

If you are not using containers, you can run the compiled executables directly:

1. Build the project:
   ```sh
   dotnet build -c Debug
   ```
2. Run the Cosmos DB seeder:
   ```sh
   .\bin\Debug\net8.0\cosmosSeered.exe --targetType cosmos --path .\AzureCosmosData
   ```
3. Run the Service Bus seeder:
   ```sh
   .\bin\Debug\net8.0\cosmosSeered.exe --targetType servicebus --path .\ServiceBusData
   ```

> **Note:**
> - You must use the `--targetType` and `--path` arguments for the executable to work.
> - You may need to adjust the path to the executable depending on your build configuration (Debug/Release) and .NET version.
> - Make sure your local Cosmos DB and Service Bus emulators are running, or update the connection strings in the code to point to your target services.
> - Use `--drop` if you want to drop and recreate containers in Cosmos DB:
>   ```sh
>   .\bin\Debug\net8.0\cosmosSeered.exe --targetType cosmos --path .\AzureCosmosData --drop
>   ```

---

For more details, see the comments in each utility class.
