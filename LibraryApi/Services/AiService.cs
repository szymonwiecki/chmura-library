// Generowanie opisów książek przez Claude (Anthropic) - model Haiku
using System.Text;
using System.Text.Json;

namespace LibraryApi.Services
{
    public class AiService : IAiService
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;

        public AiService(HttpClient http, IConfiguration config)
        {
            _http = http;
            _apiKey = config["Anthropic:ApiKey"]
                ?? throw new InvalidOperationException("Anthropic:ApiKey is not configured.");
        }

        public async Task<string> GenerateBookDescriptionAsync(string title, string author, string genre, int year)
        {
            var body = new
            {
                model = "claude-haiku-4-5-20251001",
                max_tokens = 300,
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = $"Napisz krótki, wciągający opis książki (2-3 zdania) pt. \"{title}\" autorstwa {author}, wydanej w {year} roku, z gatunku {genre}. Odpowiedz wyłącznie treścią opisu — bez wstępu, bez cudzysłowów, po polsku."
                    }
                }
            };

            var bodyJson = JsonSerializer.Serialize(body);

            // Retry z backoffem gdy API Anthropic zwróci 529 (tymczasowe przeciążenie)
            int[] delays = [2000, 5000, 10000];
            HttpResponseMessage? response = null;

            for (int attempt = 0; attempt <= delays.Length; attempt++)
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
                request.Headers.Add("x-api-key", _apiKey);
                request.Headers.Add("anthropic-version", "2023-06-01");
                request.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");

                response = await _http.SendAsync(request);

                if ((int)response.StatusCode != 529)
                    break;

                if (attempt < delays.Length)
                    await Task.Delay(delays[attempt]);
            }

            response!.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return doc.RootElement
                .GetProperty("content")[0]
                .GetProperty("text")
                .GetString() ?? string.Empty;
        }
    }
}
