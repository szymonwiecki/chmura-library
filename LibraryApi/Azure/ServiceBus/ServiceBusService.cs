// Azure Service Bus - wysyłanie zdarzeń biznesowych jako JSON
// Wiadomości odbiera Azure Function ServiceBusConsumerFunction
using Azure.Messaging.ServiceBus;

namespace LibraryApi.Azure.ServiceBus
{
    public class ServiceBusService : IServiceBusService
    {
        private readonly IConfiguration _config;
        private ServiceBusSender? _sender;

        public ServiceBusService(IConfiguration config)
        {
            _config = config;
        }

        private ServiceBusSender GetSender()
        {
            if (_sender != null) return _sender;
            var connString = _config["Azure:ServiceBus:ConnectionString"]!;
            var queueName  = _config["Azure:ServiceBus:QueueName"] ?? "book-events";
            var client = new ServiceBusClient(connString);
            _sender = client.CreateSender(queueName);
            return _sender;
        }

        public async Task SendMessageAsync(string message)
        {
            var sender = GetSender();
            await sender.SendMessageAsync(new ServiceBusMessage(message));
        }
    }
}
