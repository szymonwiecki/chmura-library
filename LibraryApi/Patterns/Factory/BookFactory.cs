// Wzorzec Factory - wybiera odpowiedni kreator na podstawie typu książki
using LibraryApi.Models;

namespace LibraryApi.Patterns.Factory
{
    public class BookFactory : IBookFactory
    {
        private static readonly Dictionary<BookType, BookCreator> _creators = new()
        {
            { BookType.Physical,  new PaperBookCreator()  },
            { BookType.Ebook,     new EbookCreator()      },
            { BookType.Audiobook, new AudiobookCreator()  }
        };

        public Book Create(string title, string author, int publishedYear, string genre, BookType type)
        {
            var creator = _creators.GetValueOrDefault(type) ?? _creators[BookType.Physical];
            return creator.Create(title, author, publishedYear, genre);
        }
    }
}
