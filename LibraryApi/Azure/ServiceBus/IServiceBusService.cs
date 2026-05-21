namespace LibraryApi.Azure.ServiceBus
{
    public interface IServiceBusService
    {
        Task SendMessageAsync(string message);
    }
}
