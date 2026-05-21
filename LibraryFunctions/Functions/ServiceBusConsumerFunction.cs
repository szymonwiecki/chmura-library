using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace LibraryFunctions.Functions
{
    // Receives book events published to Azure Service Bus by ServiceBusSubscriber (Observer)
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
            _logger.LogInformation("[ServiceBus Consumer] Received event: {Message}", message);
        }
    }
}
