namespace LibraryApi.Models
{
    public class GoogleBookDto
    {
        public string GoogleBookId  { get; set; } = string.Empty;
        public string Title         { get; set; } = string.Empty;
        public string Author        { get; set; } = string.Empty;
        public int    PublishedYear { get; set; }
        public string Genre         { get; set; } = string.Empty;
        public string Description   { get; set; } = string.Empty;
        public string ThumbnailUrl  { get; set; } = string.Empty;
    }
}
