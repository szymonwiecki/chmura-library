namespace LibraryApi.Patterns.Command
{
    public interface ICommand
    {
        string Description { get; }
        Task ExecuteAsync();
    }
}
