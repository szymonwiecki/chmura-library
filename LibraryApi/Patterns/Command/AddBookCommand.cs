using LibraryApi.Models;
using LibraryApi.Services;

namespace LibraryApi.Patterns.Command
{
    public class AddBookCommand : ICommand
    {
        private readonly IBookService _bookService;
        public Book Book { get; }

        public AddBookCommand(IBookService bookService, Book book)
        {
            _bookService = bookService;
            Book = book;
        }

        public string Description => $"Add book: '{Book.Title}' by {Book.Author}";

        public async Task ExecuteAsync() => await _bookService.CreateAsync(Book);
    }
}
