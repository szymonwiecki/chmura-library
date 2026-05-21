using LibraryApi.Data;
using LibraryApi.Models;
using LibraryApi.Patterns.Observer;
using Microsoft.EntityFrameworkCore;

namespace LibraryApi.Services
{
    // Wzorzec Event - delegate i event do notyfikacji po dodaniu książki
    public delegate void BookAddedHandler(Book book);

    public class BookService : IBookService
    {
        private readonly LibraryContext _context;
        private readonly BookEventPublisher _publisher;

        // C# event keyword - subskrybuje BookFacade i wysyła powiadomienie do Azure Queue
        public event BookAddedHandler? BookAdded;

        public BookService(LibraryContext context, BookEventPublisher publisher)
        {
            _context = context;
            _publisher = publisher;
        }

        public async Task<IEnumerable<Book>> GetAllAsync() =>
            await _context.Books.ToListAsync();

        public async Task<Book?> GetByIdAsync(int id) =>
            await _context.Books.FindAsync(id);

        public async Task<Book> CreateAsync(Book book)
        {
            _context.Books.Add(book);
            await _context.SaveChangesAsync();
            await _publisher.PublishAsync(new BookEvent { EventType = BookEventType.Added, Book = book });
            BookAdded?.Invoke(book); // wywołanie eventu - powiadamia subskrybentów
            return book;
        }

        public async Task<bool> UpdateAsync(int id, Book book)
        {
            var existing = await _context.Books.FindAsync(id);
            if (existing == null) return false;

            existing.Title         = book.Title;
            existing.Author        = book.Author;
            existing.PublishedYear = book.PublishedYear;
            existing.Genre         = book.Genre;
            existing.BookType      = book.BookType;
            existing.CoverImageUrl = book.CoverImageUrl ?? existing.CoverImageUrl;
            existing.Description   = book.Description ?? existing.Description;
            existing.Notes         = book.Notes;
            // IsFavorite zmienia się tylko przez ToggleFavoriteAsync

            await _context.SaveChangesAsync();
            await _publisher.PublishAsync(new BookEvent { EventType = BookEventType.Updated, Book = existing });
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null) return false;

            _context.Books.Remove(book);
            await _context.SaveChangesAsync();
            await _publisher.PublishAsync(new BookEvent { EventType = BookEventType.Deleted, Book = book });
            return true;
        }

        public async Task ToggleFavoriteAsync(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null) return;
            book.IsFavorite = !book.IsFavorite;
            await _context.SaveChangesAsync();
        }
    }
}
