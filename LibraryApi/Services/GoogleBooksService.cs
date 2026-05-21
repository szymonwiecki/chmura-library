// Integracja z Google Books API - wyszukiwanie książek po tytule/autorze/gatunku
using LibraryApi.Models;
using System.Text.Json;

namespace LibraryApi.Services
{
    public class GoogleBooksService : IGoogleBooksService
    {
        private readonly HttpClient _http;
        private readonly string? _apiKey;

        public GoogleBooksService(HttpClient http, IConfiguration config)
        {
            _http   = http;
            _apiKey = config["GoogleBooks:ApiKey"]; // klucz opcjonalny; bez niego limit 1000 req/dzień
        }

        // Wyszukuje książki przez Google Books API; zwraca max 10 wyników
        public async Task<IEnumerable<GoogleBookDto>> SearchAsync(string query)
        {
            var url = $"https://www.googleapis.com/books/v1/volumes?q={Uri.EscapeDataString(query)}&maxResults=10";
            if (!string.IsNullOrEmpty(_apiKey))
                url += $"&key={_apiKey}";

            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Google Books API returned {(int)response.StatusCode} {response.ReasonPhrase}");

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            if (!doc.RootElement.TryGetProperty("items", out var items)) return [];

            var results = new List<GoogleBookDto>();
            foreach (var item in items.EnumerateArray())
            {
                if (!item.TryGetProperty("volumeInfo", out var info)) continue;

                var title = info.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";
                if (string.IsNullOrEmpty(title)) continue;

                var author = "";
                if (info.TryGetProperty("authors", out var authors) && authors.GetArrayLength() > 0)
                    author = authors[0].GetString() ?? "";

                var year = 0;
                if (info.TryGetProperty("publishedDate", out var dateEl))
                {
                    var d = dateEl.GetString() ?? "";
                    if (d.Length >= 4) int.TryParse(d[..4], out year);
                }

                var genre = "";
                if (info.TryGetProperty("categories", out var cats) && cats.GetArrayLength() > 0)
                    genre = cats[0].GetString() ?? "";

                var description = info.TryGetProperty("description", out var desc)
                    ? desc.GetString() ?? "" : "";

                var thumbnail = "";
                if (info.TryGetProperty("imageLinks", out var imgs))
                {
                    if (imgs.TryGetProperty("thumbnail", out var th)) thumbnail = th.GetString() ?? "";
                    else if (imgs.TryGetProperty("smallThumbnail", out var sm)) thumbnail = sm.GetString() ?? "";
                    // Google Books zwraca http:// — konwertujemy na https://
                    thumbnail = thumbnail.Replace("http://", "https://");
                }

                results.Add(new GoogleBookDto
                {
                    GoogleBookId  = item.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "",
                    Title         = title,
                    Author        = author,
                    PublishedYear = year,
                    Genre         = genre,
                    Description   = description,
                    ThumbnailUrl  = thumbnail
                });
            }

            return results;
        }
    }
}
