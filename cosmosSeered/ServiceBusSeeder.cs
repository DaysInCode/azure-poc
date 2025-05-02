using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;

namespace cosmosSeered;

public static class ServiceBusSeeder
{
    // Adjust connection string for your emulator if needed
    private const string ServiceBusConnectionString = "Endpoint=sb://host.docker.internal;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";


    public static async Task RunAsync(string parentPath, ILogger logger)
    {
        var queueDir = Path.Combine(parentPath, "queue");
        var topicDir = Path.Combine(parentPath, "topic");
        if (!Directory.Exists(queueDir) && !Directory.Exists(topicDir))
        {
            logger.LogError("No queue or topic directories found in {Path}", parentPath);
            return;
        }
        var client = new ServiceBusClient(ServiceBusConnectionString);
        if (Directory.Exists(queueDir))
        {
            foreach (var file in Directory.GetFiles(queueDir, "*.json"))
            {
                await SendMessageFromFile(client, file, isQueue: true, logger);
            }
        }
        if (Directory.Exists(topicDir))
        {
            foreach (var file in Directory.GetFiles(topicDir, "*.json"))
            {
                await SendMessageFromFile(client, file, isQueue: false, logger);
            }
        }
    }

    private static async Task SendMessageFromFile(ServiceBusClient client, string file, bool isQueue, ILogger logger)
    {
        try
        {
            var json = await File.ReadAllTextAsync(file);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var def = root.GetProperty("defintion");
            var name = def.GetProperty("topicName").GetString() ?? throw new Exception("Missing topicName");
            var customProps = root.GetProperty("msgCustomProperties");
            var msgData = root.GetProperty("msgData").GetRawText();
            var sender = client.CreateSender(name);
            var msg = new ServiceBusMessage(msgData);
            foreach (var prop in customProps.EnumerateObject())
            {
                msg.ApplicationProperties[prop.Name] = prop.Value.GetString();
            }
            await sender.SendMessageAsync(msg);
            logger.LogInformation("Seeded {Type} '{Name}' from {File}", isQueue ? "queue" : "topic", name, file);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to seed message from {File}", file);
        }
    }
}
