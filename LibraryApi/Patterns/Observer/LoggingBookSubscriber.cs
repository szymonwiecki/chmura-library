// Wzorzec Observer - subskrybent logujący zdarzenia CRUD do logów aplikacji
namespace LibraryApi.Patterns.Observer
{
    public class LoggingBookSubscriber : IBookEventSubscriber
    {
        private readonly ILogger<LoggingBookSubscriber> _logger;

        public LoggingBookSubscriber(ILogger<LoggingBookSubscriber> logger)
        {
            _logger = logger;
        }

        public Task OnBookEventAsync(BookEvent bookEvent)
        {
            _logger.LogInformation("[Observer] Book {EventType}: '{Title}' by {Author} at {OccurredAt}",
                bookEvent.EventType, bookEvent.Book.Title, bookEvent.Book.Author, bookEvent.OccurredAt);
            return Task.CompletedTask;
        }
    }
}
