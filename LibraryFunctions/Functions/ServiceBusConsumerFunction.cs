using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LibraryFunctions.Functions
{
    // Odbiera zdarzenia biznesowe z Azure Service Bus (wzorzec Observer)
    // Obsługuje: Added, Updated, Deleted, StatusChanged
    public class ServiceBusConsumerFunction
    {
        private readonly ILogger<ServiceBusConsumerFunction> _logger;

        public ServiceBusConsumerFunction(ILogger<ServiceBusConsumerFunction> logger)
        {
            _logger = logger;
        }

        [Function("ServiceBusConsumerFunction")]
        public void Run(
            [ServiceBusTrigger("book-events", Connection = "ServiceBusConnection")] string message)
        {
            try
            {
                using var doc = JsonDocument.Parse(message);
                var root = doc.RootElement;

                var eventType    = root.GetProperty("EventType").GetString() ?? "Unknown";
                var bookTitle    = root.GetProperty("BookTitle").GetString() ?? "Unknown";
                var readingStatus = root.TryGetProperty("ReadingStatus", out var statusEl)
                    ? statusEl.GetString()
                    : null;

                if (eventType == "StatusChanged")
                    _logger.LogInformation("[ServiceBus] '{Title}' reading status changed → {Status}", bookTitle, readingStatus);
                else
                    _logger.LogInformation("[ServiceBus] Book '{Title}': {EventType}", bookTitle, eventType);
            }
            catch (JsonException)
            {
                _logger.LogWarning("[ServiceBus] Could not parse message as JSON: {Message}", message);
            }
        }
    }
}
