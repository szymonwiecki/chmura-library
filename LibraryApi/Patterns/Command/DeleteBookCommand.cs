using LibraryApi.Services;

namespace LibraryApi.Patterns.Command
{
    public class DeleteBookCommand : ICommand
    {
        private readonly IBookService _bookService;
        private readonly int _id;

        public DeleteBookCommand(IBookService bookService, int id)
        {
            _bookService = bookService;
            _id = id;
        }

        public string Description => $"Delete book ID={_id}";

        public async Task ExecuteAsync() => await _bookService.DeleteAsync(_id);
    }
}
