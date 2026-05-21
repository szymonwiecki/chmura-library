using LibraryApi.Models;

namespace LibraryApi.Patterns.Factory
{
    public interface IBookFactory
    {
        Book Create(string title, string author, int publishedYear, string genre, BookType type);
    }
}
