namespace LibraryApi.Services
{
    public interface IAiService
    {
        Task<string> GenerateBookDescriptionAsync(string title, string author, string genre, int year);
    }
}
