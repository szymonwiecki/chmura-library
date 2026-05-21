using Azure;
using Azure.Data.Tables;

namespace LibraryApi.Patterns.Command
{
    public class CommandHistoryEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = "commands";
        public string RowKey       { get; set; } = string.Empty;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string Description  { get; set; } = string.Empty;
        public DateTimeOffset ExecutedAt { get; set; }
    }
}
