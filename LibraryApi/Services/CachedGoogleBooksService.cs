// Wzorzec Proxy - opakowuje GoogleBooksService i cache'uje wyniki w Redis (TTL 10 min)
using LibraryApi.Models;
using StackExchange.Redis;
using System.Text.Json;

namespace LibraryApi.Services
{
    public class CachedGoogleBooksService : IGoogleBooksService
    {
        private readonly IGoogleBooksService _inner;
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<CachedGoogleBooksService> _logger;
        private readonly TimeSpan _expiry = TimeSpan.FromMinutes(10);

        public CachedGoogleBooksService(
            IGoogleBooksService inner,
            IConnectionMultiplexer redis,
            ILogger<CachedGoogleBooksService> logger)
        {
            _inner  = inner;
            _redis  = redis;
            _logger = logger;
        }

        private bool IsAvailable => _redis.IsConnected;
        private IDatabase Db => _redis.GetDatabase();

        public async Task<IEnumerable<GoogleBookDto>> SearchAsync(string query)
        {
            if (!IsAvailable) return await _inner.SearchAsync(query);

            var key = $"googlebooks:{query.ToLowerInvariant().Trim()}";

            try
            {
                var cached = await Db.StringGetAsync(key);
                if (cached.HasValue)
                    return JsonSerializer.Deserialize<List<GoogleBookDto>>(cached!)!;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Redis] Google Books cache read failed — bypassing cache.");
            }

            var results = await _inner.SearchAsync(query);

            try
            {
                await Db.StringSetAsync(key, JsonSerializer.Serialize(results), _expiry);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Redis] Google Books cache write failed.");
            }

            return results;
        }
    }
}
