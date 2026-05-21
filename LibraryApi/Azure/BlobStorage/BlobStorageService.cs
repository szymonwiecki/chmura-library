// Azure Blob Storage - przechowywanie okładek książek
// Upload do kontenera "book-covers", zwracany SAS URL ważny 10 lat
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace LibraryApi.Azure.BlobStorage
{
    public class BlobStorageService : IBlobStorageService
    {
        private readonly string _connectionString;
        private readonly string _containerName;
        private BlobContainerClient? _container;

        public BlobStorageService(IConfiguration config)
        {
            _connectionString = config["Azure:BlobStorage:ConnectionString"]!;
            _containerName    = config["Azure:BlobStorage:ContainerName"] ?? "book-covers";
        }

        private async Task<BlobContainerClient> GetContainerAsync()
        {
            if (_container != null) return _container;

            var client = new BlobContainerClient(_connectionString, _containerName);

            try
            {
                await client.CreateIfNotExistsAsync(PublicAccessType.Blob);
            }
            catch (RequestFailedException)
            {
                // Konto ma wyłączony anonymous access - tworzymy prywatny kontener
                await client.CreateIfNotExistsAsync(PublicAccessType.None);
            }

            _container = client;
            return _container;
        }

        public async Task<string> UploadAsync(Stream stream, string fileName, string contentType)
        {
            var container = await GetContainerAsync();
            var blobName = $"{Guid.NewGuid()}-{fileName}";
            var blob = container.GetBlobClient(blobName);
            await blob.UploadAsync(stream, new BlobHttpHeaders { ContentType = contentType });

            // SAS URL z uprawnieniem read na 10 lat - działa nawet przy prywatnym kontenerze
            var sasUri = blob.GenerateSasUri(
                BlobSasPermissions.Read,
                DateTimeOffset.UtcNow.AddYears(10));
            return sasUri.ToString();
        }

        public async Task<string> UploadTextAsync(string content, string fileName, string contentType)
        {
            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
            return await UploadAsync(stream, fileName, contentType);
        }

        public async Task DeleteAsync(string blobName)
        {
            var container = await GetContainerAsync();
            var blob = container.GetBlobClient(blobName);
            await blob.DeleteIfExistsAsync();
        }
    }
}
