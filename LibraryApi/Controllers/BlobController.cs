// Azure Blob Storage - upload okładek książek
using LibraryApi.Azure.BlobStorage;
using LibraryApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace LibraryApi.Controllers
{
    [Route("api/books/{id}/cover")]
    [ApiController]
    [Authorize]
    public class BlobController : ControllerBase
    {
        private readonly IBlobStorageService _blobService;
        private readonly IBookService _bookService;

        public BlobController(IBlobStorageService blobService, IBookService bookService)
        {
            _blobService = blobService;
            _bookService = bookService;
        }

        [HttpPost]
        [SwaggerOperation(Summary = "Upload book cover", Description = "Uploads a cover image to Azure Blob Storage and saves the URL in the book record.")]
        public async Task<IActionResult> UploadCover(int id, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file provided" });

            try
            {
                using var stream = file.OpenReadStream();
                var url = await _blobService.UploadAsync(stream, file.FileName, file.ContentType);

                var book = await _bookService.GetByIdAsync(id);
                if (book != null)
                {
                    book.CoverImageUrl = url;
                    await _bookService.UpdateAsync(id, book);
                }

                return Ok(new { coverUrl = url });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Cover upload failed", details = ex.Message });
            }
        }
    }
}
