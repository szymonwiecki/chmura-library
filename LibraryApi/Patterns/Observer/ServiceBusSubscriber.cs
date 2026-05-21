// Wzorzec Observer - subskrybent wysyłający zdarzenie jako JSON do Azure Service Bus
using LibraryApi.Azure.ServiceBus;
using System.Text.Json;

namespace LibraryApi.Patterns.Observer
{
    public class ServiceBusSubscriber : IBookEventSubscriber
    {
        private readonly IServiceBusService _serviceBus;

        public ServiceBusSubscriber(IServiceBusService serviceBus)
        {
            _serviceBus = serviceBus;
        }

        public async Task OnBookEventAsync(BookEvent bookEvent)
        {
            var payload = JsonSerializer.Serialize(new
            {
                EventType = bookEvent.EventType.ToString(),
                BookId = bookEvent.Book.Id,
                BookTitle = bookEvent.Book.Title,
                ReadingStatus = bookEvent.Book.ReadingStatus.ToString(),
                OccurredAt = bookEvent.OccurredAt
            });
            await _serviceBus.SendMessageAsync(payload);
        }
    }
}
