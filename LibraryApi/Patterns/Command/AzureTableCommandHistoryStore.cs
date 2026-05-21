// Wzorzec Command - zapis historii komend w Azure Table Storage
using Azure.Data.Tables;

namespace LibraryApi.Patterns.Command
{
    public class AzureTableCommandHistoryStore : ICommandHistoryStore
    {
        private readonly string _connectionString;
        private readonly string _tableName;
        private TableClient? _client;

        public AzureTableCommandHistoryStore(IConfiguration config)
        {
            _connectionString = config["Azure:TableStorage:ConnectionString"]!;
            _tableName        = config["Azure:TableStorage:TableName"] ?? "commandhistory";
        }

        private async Task<TableClient> GetClientAsync()
        {
            if (_client != null) return _client;
            var client = new TableClient(_connectionString, _tableName);
            await client.CreateIfNotExistsAsync();
            _client = client;
            return _client;
        }

        public async Task RecordAsync(string description)
        {
            var client = await GetClientAsync();

            // Odwrócony tick jako RowKey - najnowsze wpisy pojawiają się pierwsze przy sortowaniu
            var rowKey = (DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks).ToString("D20")
                         + "_" + Guid.NewGuid().ToString("N")[..6];

            var entity = new CommandHistoryEntity
            {
                PartitionKey = "commands",
                RowKey       = rowKey,
                Description  = description,
                ExecutedAt   = DateTimeOffset.UtcNow
            };

            await client.AddEntityAsync(entity);
        }

        public async Task<IEnumerable<CommandHistoryEntry>> GetAllAsync()
        {
            var client = await GetClientAsync();
            var entries = new List<CommandHistoryEntry>();

            await foreach (var entity in client.QueryAsync<CommandHistoryEntity>(
                filter: "PartitionKey eq 'commands'",
                maxPerPage: 100))
            {
                entries.Add(new CommandHistoryEntry
                {
                    Description = entity.Description,
                    ExecutedAt  = entity.ExecutedAt.UtcDateTime
                });
            }

            return entries;
        }
    }
}
