using LibraryApi.Models;

namespace LibraryApi.Patterns.Observer
{
    public class BookEvent
    {
        public BookEventType EventType { get; init; }
        public Book Book { get; init; } = null!;
        public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    }
}
