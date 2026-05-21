// Azure Queue Storage - producent wiadomości (wzorzec Producer-Consumer)
// Wiadomości odbiera Azure Function BookNotificationFunction
using Azure.Storage.Queues;
using System.Text;

namespace LibraryApi.Azure.QueueStorage
{
    public class QueueService : IQueueService
    {
        private readonly string _connectionString;
        private readonly string _queueName;

        public QueueService(IConfiguration config)
        {
            _connectionString = config["Azure:QueueStorage:ConnectionString"]!;
            _queueName = config["Azure:QueueStorage:QueueName"] ?? "book-notifications";
        }

        private QueueClient GetClient()
        {
            var client = new QueueClient(_connectionString, _queueName);
            client.CreateIfNotExists();
            return client;
        }

        public async Task EnqueueAsync(string message)
        {
            var client = GetClient();
            // Azure Queue Storage wymaga base64
            var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(message));
            await client.SendMessageAsync(encoded);
        }
    }
}
