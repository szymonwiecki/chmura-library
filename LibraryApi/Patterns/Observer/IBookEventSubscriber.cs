namespace LibraryApi.Patterns.Observer
{
    public interface IBookEventSubscriber
    {
        Task OnBookEventAsync(BookEvent bookEvent);
    }
}
