// Wzorzec Factory - abstrakcyjny kreator książek
using LibraryApi.Models;

namespace LibraryApi.Patterns.Factory
{
    public abstract class BookCreator
    {
        public abstract BookType BookType { get; }

        public virtual Book Create(string title, string author, int year, string genre) =>
            new Book { Title = title, Author = author, PublishedYear = year, Genre = genre, BookType = BookType };
    }
}
