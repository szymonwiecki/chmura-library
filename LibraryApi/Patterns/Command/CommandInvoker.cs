// Wzorzec Command - wykonuje komendy i zapisuje historię w Azure Table Storage
namespace LibraryApi.Patterns.Command
{
    public class CommandInvoker
    {
        private readonly ICommandHistoryStore _store;
        private readonly ILogger<CommandInvoker> _logger;

        public CommandInvoker(ICommandHistoryStore store, ILogger<CommandInvoker> logger)
        {
            _store  = store;
            _logger = logger;
        }

        public async Task ExecuteAsync(ICommand command)
        {
            await command.ExecuteAsync();

            // Zapis historii nie może crashować operacji CRUD
            try
            {
                await _store.RecordAsync(command.Description);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CommandHistory] Failed to persist command to Azure Tables.");
            }
        }
    }
}
