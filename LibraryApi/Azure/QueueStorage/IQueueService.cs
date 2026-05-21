namespace LibraryApi.Azure.QueueStorage
{
    public interface IQueueService
    {
        Task EnqueueAsync(string message);
    }
}
