using LibraryApi.Models;

namespace LibraryApi.Patterns.Factory
{
    public class PaperBookCreator : BookCreator
    {
        public override BookType BookType => BookType.Physical;
    }
}
