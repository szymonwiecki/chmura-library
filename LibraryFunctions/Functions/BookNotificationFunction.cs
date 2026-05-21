using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text;

namespace LibraryFunctions.Functions
{
    // Consumer side of Producer-Consumer pattern
    // Triggered by Azure Queue Storage messages sent from LibraryApi (QueueService)
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
            // Messages from QueueService are base64-encoded
            string decoded;
            try
            {
                decoded = Encoding.UTF8.GetString(Convert.FromBase64String(message));
            }
            catch
            {
                decoded = message;
            }

            _logger.LogInformation("[Consumer] Processing queue message: {Message}", decoded);

            var parts = decoded.Split(':', 3);
            if (parts.Length == 3)
            {
                var eventType = parts[0];
                var bookId = parts[1];
                var bookTitle = parts[2];

                _logger.LogInformation("[Consumer] Event={EventType}, BookId={BookId}, Title={BookTitle}",
                    eventType, bookId, bookTitle);

                SendNotificationEmail(eventType, bookTitle);
            }
        }

        private void SendNotificationEmail(string eventType, string bookTitle)
        {
            // Miejsce na integrację z SendGrid / Azure Communication Services
            _logger.LogInformation("[Email] Book '{BookTitle}' was {EventType} — notification sent.",
                bookTitle, eventType.ToLower());
        }
    }
}
