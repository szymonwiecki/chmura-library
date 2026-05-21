using LibraryApi.Models;

namespace LibraryApi.Services
{
    public interface IGoogleBooksService
    {
        Task<IEnumerable<GoogleBookDto>> SearchAsync(string query);
    }
}
