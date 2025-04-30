namespace CosmosInitializer.Models
{
    public class CosmosDbConfig
    {
        public string Endpoint { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public List<DatabaseConfig> Databases { get; set; } = new List<DatabaseConfig>();
        public ServiceBusConfig? ServiceBus { get; set; }
    }

    public class DatabaseConfig
    {
        public string Name { get; set; } = string.Empty;
        public List<ContainerConfig> Containers { get; set; } = new List<ContainerConfig>();
    }

    public class ContainerConfig
    {
        public string Name { get; set; } = string.Empty;
        public string PartitionKeyPath { get; set; } = string.Empty;
        public string SchemaVersion { get; set; } = "v1"; // Default schema version
    }

    public class ServiceBusConfig
    {
        public List<string>? Topics { get; set; }
    }

    public class DocumentHistory
    {
        public string Id { get; set; } = string.Empty;
        public string Timestamp { get; set; } = DateTime.UtcNow.ToString("o");
        public string UpdateType { get; set; } = "update"; // Can be create/update/delete
        public object PreviousDocument { get; set; } = new object();
    }
}