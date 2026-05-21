// Wzorzec Observer - publisher rozgłasza zdarzenia CRUD do subskrybentów
namespace LibraryApi.Patterns.Observer
{
    public class BookEventPublisher
    {
        private readonly List<IBookEventSubscriber> _subscribers = new();
        private readonly ILogger<BookEventPublisher> _logger;

        public BookEventPublisher(ILogger<BookEventPublisher> logger)
        {
            _logger = logger;
        }

        public void Subscribe(IBookEventSubscriber subscriber) => _subscribers.Add(subscriber);
        public void Unsubscribe(IBookEventSubscriber subscriber) => _subscribers.Remove(subscriber);

        // Powiadamia wszystkich subskrybentów; błąd jednego nie blokuje pozostałych
        public async Task PublishAsync(BookEvent bookEvent)
        {
            foreach (var subscriber in _subscribers)
            {
                try
                {
                    await subscriber.OnBookEventAsync(bookEvent);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "[Observer] Subscriber {Subscriber} threw on event {EventType} for book '{Title}'",
                        subscriber.GetType().Name, bookEvent.EventType, bookEvent.Book.Title);
                }
            }
        }
    }
}
