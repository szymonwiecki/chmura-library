namespace LibraryApi.Azure.BlobStorage
{
    public interface IBlobStorageService
    {
        Task<string> UploadAsync(Stream stream, string fileName, string contentType);
        Task<string> UploadTextAsync(string content, string fileName, string contentType);
        Task DeleteAsync(string blobName);
    }
}
