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
    }

    public class ServiceBusConfig
    {
        public List<string>? Topics { get; set; }
    }
}