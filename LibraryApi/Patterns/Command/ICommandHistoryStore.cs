namespace LibraryApi.Patterns.Command
{
    public interface ICommandHistoryStore
    {
        Task RecordAsync(string description);
        Task<IEnumerable<CommandHistoryEntry>> GetAllAsync();
    }
}
