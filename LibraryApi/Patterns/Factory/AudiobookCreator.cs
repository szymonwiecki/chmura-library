using LibraryApi.Models;

namespace LibraryApi.Patterns.Factory
{
    public class AudiobookCreator : BookCreator
    {
        public override BookType BookType => BookType.Audiobook;
    }
}
