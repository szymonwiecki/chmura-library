using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text;

namespace LibraryFunctions.Functions
{
    // Przetwarza zlecenia eksportu z Azure Queue Storage
    // Wiadomość format: EXPORT:{count}:{blobUrl}
    public class BookNotificationFunction
    {
        private readonly ILogger<BookNotificationFunction> _logger;

        public BookNotificationFunction(ILogger<BookNotificationFunction> logger)
        {
            _logger = logger;
        }

        [Function("BookNotificationFunction")]
        public void Run(
            [QueueTrigger("book-notifications", Connection = "AzureWebJobsStorage")] string message)
        {
            string decoded;
            try
            {
                decoded = Encoding.UTF8.GetString(Convert.FromBase64String(message));
            }
            catch
            {
                decoded = message;
            }

            if (decoded.StartsWith("EXPORT:"))
            {
                var parts = decoded.Split(':', 3);
                if (parts.Length == 3 && int.TryParse(parts[1], out var count))
                {
                    _logger.LogInformation("[Export] Job processed: {Count} books exported. File available at: {Url}", count, parts[2]);
                }
                else
                {
                    _logger.LogWarning("[Export] Malformed export message: {Message}", decoded);
                }
            }
            else
            {
                _logger.LogInformation("[Queue] Unrecognized message: {Message}", decoded);
            }
        }
    }
}
