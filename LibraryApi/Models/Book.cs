using System.ComponentModel.DataAnnotations;
namespace LibraryApi.Models
{
    public class Book
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(100, ErrorMessage = "Title cannot be longer than 100 characters")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Author is required")]
        [StringLength(100, ErrorMessage = "Author name cannot be longer than 100 characters")]
        public string Author { get; set; } = string.Empty;

        [Range(1, 9999, ErrorMessage = "Published year must be between 1 and 9999")]
        public int PublishedYear { get; set; }

        [Required(ErrorMessage = "Genre is required")]
        [StringLength(50, ErrorMessage = "Genre cannot be longer than 50 characters")]
        public string Genre { get; set; } = string.Empty;

        public BookType BookType { get; set; } = BookType.Physical;

        public string? CoverImageUrl { get; set; }

        public string? Description { get; set; }

        public string? Notes { get; set; }

        public bool IsFavorite { get; set; } = false;

        public ReadingStatus ReadingStatus { get; set; } = ReadingStatus.None;
    }
}
