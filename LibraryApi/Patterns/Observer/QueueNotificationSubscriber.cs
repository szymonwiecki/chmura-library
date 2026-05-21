// Wzorzec Observer - subskrybent wysyłający powiadomienie do Azure Queue Storage
using LibraryApi.Azure.QueueStorage;

namespace LibraryApi.Patterns.Observer
{
    public class QueueNotificationSubscriber : IBookEventSubscriber
    {
        private readonly IQueueService _queueService;

        public QueueNotificationSubscriber(IQueueService queueService)
        {
            _queueService = queueService;
        }

        public async Task OnBookEventAsync(BookEvent bookEvent)
        {
            var message = $"{bookEvent.EventType}:{bookEvent.Book.Id}:{bookEvent.Book.Title}";
            await _queueService.EnqueueAsync(message);
        }
    }
}
