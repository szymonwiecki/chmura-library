// Wzorzec Proxy - opakowuje BookService i dodaje Redis cache (TTL 5 min)
// Jeśli Redis jest niedostępny, transparentnie przechodzi do bazy danych
using LibraryApi.Models;
using StackExchange.Redis;
using System.Text.Json;

namespace LibraryApi.Services
{
    public class CachedBookService : IBookService
    {
        private readonly IBookService _inner;
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<CachedBookService> _logger;
        private readonly TimeSpan _expiry = TimeSpan.FromMinutes(5);

        public CachedBookService(IBookService inner, IConnectionMultiplexer redis, ILogger<CachedBookService> logger)
        {
            _inner = inner;
            _redis = redis;
            _logger = logger;
        }

        private bool IsAvailable => _redis.IsConnected;
        private IDatabase Db => _redis.GetDatabase();

        public async Task<IEnumerable<Book>> GetAllAsync()
        {
            if (!IsAvailable) return await _inner.GetAllAsync();
            try
            {
                const string key = "books:all";
                var cached = await Db.StringGetAsync(key);
                if (cached.HasValue)
                    return JsonSerializer.Deserialize<List<Book>>(cached!)!;

                var books = await _inner.GetAllAsync();
                await Db.StringSetAsync(key, JsonSerializer.Serialize(books), _expiry);
                return books;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Redis] GetAllAsync cache miss — falling back to DB.");
                return await _inner.GetAllAsync();
            }
        }

        public async Task<Book?> GetByIdAsync(int id)
        {
            if (!IsAvailable) return await _inner.GetByIdAsync(id);
            try
            {
                var key = $"books:{id}";
                var cached = await Db.StringGetAsync(key);
                if (cached.HasValue)
                    return JsonSerializer.Deserialize<Book>(cached!)!;

                var book = await _inner.GetByIdAsync(id);
                if (book != null)
                    await Db.StringSetAsync(key, JsonSerializer.Serialize(book), _expiry);
                return book;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Redis] GetByIdAsync({Id}) cache miss — falling back to DB.", id);
                return await _inner.GetByIdAsync(id);
            }
        }

        public async Task<Book> CreateAsync(Book book)
        {
            var result = await _inner.CreateAsync(book);
            await TryInvalidateAsync();
            return result;
        }

        public async Task<bool> UpdateAsync(int id, Book book)
        {
            var result = await _inner.UpdateAsync(id, book);
            if (result) await TryInvalidateAsync(id);
            return result;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var result = await _inner.DeleteAsync(id);
            if (result) await TryInvalidateAsync(id);
            return result;
        }

        public async Task ToggleFavoriteAsync(int id)
        {
            await _inner.ToggleFavoriteAsync(id);
            await TryInvalidateAsync(id);
        }

        public async Task UpdateStatusAsync(int id, ReadingStatus status)
        {
            await _inner.UpdateStatusAsync(id, status);
            await TryInvalidateAsync(id);
        }

        // Inwalidacja cache po operacji zapisu
        private async Task TryInvalidateAsync(int? id = null)
        {
            if (!IsAvailable) return;
            try
            {
                await Db.KeyDeleteAsync("books:all");
                if (id.HasValue)
                    await Db.KeyDeleteAsync($"books:{id}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Redis] Cache invalidation failed — ignored.");
            }
        }
    }
}
