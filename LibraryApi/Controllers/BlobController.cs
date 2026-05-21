// Azure Blob Storage - upload okładek przez BookFacade (wzorzec Facade)
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
        private readonly BookFacade _facade; // Facade łączy BookService + BlobStorageService

        public BlobController(BookFacade facade)
        {
            _facade = facade;
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
                var url = await _facade.UploadCoverAsync(id, stream, file.FileName, file.ContentType);
                return Ok(new { coverUrl = url });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Cover upload failed", details = ex.Message });
            }
        }
    }
}
