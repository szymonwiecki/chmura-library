// Wzorzec Facade - upraszcza interfejs łącząc BookService + BlobStorageService
using LibraryApi.Azure.BlobStorage;
using LibraryApi.Models;

namespace LibraryApi.Services
{
    public class BookFacade
    {
        private readonly BookService _bookService;
        private readonly IBlobStorageService _blobService;

        public BookFacade(BookService bookService, IBlobStorageService blobService)
        {
            _bookService = bookService;
            _blobService = blobService;
        }

        // Fasada: dodanie książki razem z okładką w jednej operacji
        public async Task<Book> AddBookWithCoverAsync(Book book, Stream? coverStream, string? fileName, string? contentType)
        {
            var created = await _bookService.CreateAsync(book);

            if (coverStream != null && fileName != null && contentType != null)
            {
                var url = await _blobService.UploadAsync(coverStream, fileName, contentType);
                created.CoverImageUrl = url;
                await _bookService.UpdateAsync(created.Id, created);
            }

            return created;
        }

        // Fasada: upload okładki dla istniejącej książki
        public async Task<string> UploadCoverAsync(int bookId, Stream stream, string fileName, string contentType)
        {
            var url = await _blobService.UploadAsync(stream, fileName, contentType);
            var book = await _bookService.GetByIdAsync(bookId);
            if (book != null)
            {
                book.CoverImageUrl = url;
                await _bookService.UpdateAsync(bookId, book);
            }
            return url;
        }
    }
}
