using LibraryApi.Models;
using LibraryApi.Services;

namespace LibraryApi.Patterns.Command
{
    public class UpdateBookCommand : ICommand
    {
        private readonly IBookService _bookService;
        private readonly int _id;
        private readonly Book _book;

        public UpdateBookCommand(IBookService bookService, int id, Book book)
        {
            _bookService = bookService;
            _id = id;
            _book = book;
        }

        public string Description => $"Update book ID={_id}: '{_book.Title}'";

        public async Task ExecuteAsync() => await _bookService.UpdateAsync(_id, _book);
    }
}
