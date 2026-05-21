using LibraryApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace LibraryApi.Controllers
{
    [Route("api/googlebooks")]
    [ApiController]
    [Authorize]
    public class GoogleBooksController : ControllerBase
    {
        private readonly IGoogleBooksService _googleBooks;

        public GoogleBooksController(IGoogleBooksService googleBooks)
        {
            _googleBooks = googleBooks;
        }

        [HttpGet("search")]
        [SwaggerOperation(Summary = "Search Google Books", Description = "Searches Google Books API; results are cached in Redis (Proxy pattern).")]
        public async Task<IActionResult> Search([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest(new { message = "Query is required" });

            try
            {
                var results = await _googleBooks.SearchAsync(q);
                return Ok(results);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Google Books search failed", details = ex.Message });
            }
        }
    }
}
