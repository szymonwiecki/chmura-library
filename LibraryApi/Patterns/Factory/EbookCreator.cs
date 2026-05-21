using LibraryApi.Models;

namespace LibraryApi.Patterns.Factory
{
    public class EbookCreator : BookCreator
    {
        public override BookType BookType => BookType.Ebook;
    }
}
