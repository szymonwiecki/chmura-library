namespace LibraryApi.Patterns.Command
{
    public class CommandHistoryEntry
    {
        public string Description { get; set; } = string.Empty;
        public DateTime ExecutedAt { get; set; }
    }
}
