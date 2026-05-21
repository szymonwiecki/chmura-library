using LibraryApi.Models;
using LibraryApi.Patterns.Command;
using LibraryApi.Patterns.Factory;
using LibraryApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace LibraryApi.Controllers
{
    public record GenerateDescriptionRequest(string Title, string Author, string Genre, int Year);

    [Route("api/[controller]")]
    [ApiController]
    public class BooksController : ControllerBase
    {
        private readonly IBookService _bookService;          // wzorzec Proxy: CachedBookService → BookService
        private readonly CommandInvoker _invoker;            // wzorzec Command
        private readonly IBookFactory _factory;              // wzorzec Factory
        private readonly ICommandHistoryStore _historyStore; // Azure Table Storage
        private readonly IAiService _aiService;              // Claude AI

        public BooksController(
            IBookService bookService,
            CommandInvoker invoker,
            IBookFactory factory,
            ICommandHistoryStore historyStore,
            IAiService aiService)
        {
            _bookService  = bookService;
            _invoker      = invoker;
            _factory      = factory;
            _historyStore = historyStore;
            _aiService    = aiService;
        }

        [Authorize]
        [HttpGet]
        [SwaggerOperation(Summary = "Show book list")]
        public async Task<ActionResult<IEnumerable<Book>>> GetBooks()
        {
            try
            {
                return Ok(await _bookService.GetAllAsync());
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching books", details = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "Show selected book")]
        public async Task<ActionResult<Book>> GetBook(int id)
        {
            try
            {
                var book = await _bookService.GetByIdAsync(id);
                if (book == null) return NotFound(new { message = "Book not found" });
                return Ok(book);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching book", details = ex.Message });
            }
        }

        // Wzorzec Factory tworzy obiekt Book, wzorzec Command wykonuje operację i zapisuje historię
        [Authorize]
        [HttpPost]
        [SwaggerOperation(Summary = "Add a new book")]
        public async Task<ActionResult<Book>> CreateBook(Book book)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var newBook = _factory.Create(book.Title, book.Author, book.PublishedYear, book.Genre, book.BookType);
                newBook.Description   = book.Description;
                newBook.Notes         = book.Notes;
                newBook.CoverImageUrl = book.CoverImageUrl;

                var command = new AddBookCommand(_bookService, newBook);
                await _invoker.ExecuteAsync(command);

                return CreatedAtAction(nameof(GetBook), new { id = newBook.Id }, newBook);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error creating book", details = ex.Message });
            }
        }

        [Authorize]
        [HttpPut("{id}")]
        [SwaggerOperation(Summary = "Update existing book")]
        public async Task<ActionResult> UpdateBook(int id, Book book)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (id != book.Id) return BadRequest(new { message = "ID in URL and body must match" });

            try
            {
                var existing = await _bookService.GetByIdAsync(id);
                if (existing == null) return NotFound(new { message = "Book not found" });

                var command = new UpdateBookCommand(_bookService, id, book);
                await _invoker.ExecuteAsync(command);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating book", details = ex.Message });
            }
        }

        [Authorize]
        [HttpDelete("{id}")]
        [SwaggerOperation(Summary = "Remove a book")]
        public async Task<ActionResult> DeleteBook(int id)
        {
            try
            {
                var existing = await _bookService.GetByIdAsync(id);
                if (existing == null) return NotFound(new { message = "Book not found" });

                var command = new DeleteBookCommand(_bookService, id);
                await _invoker.ExecuteAsync(command);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting book", details = ex.Message });
            }
        }

        [Authorize]
        [HttpPatch("{id}/favorite")]
        [SwaggerOperation(Summary = "Toggle favorite")]
        public async Task<ActionResult> ToggleFavorite(int id)
        {
            try
            {
                var existing = await _bookService.GetByIdAsync(id);
                if (existing == null) return NotFound(new { message = "Book not found" });
                await _bookService.ToggleFavoriteAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error toggling favorite", details = ex.Message });
            }
        }

        // Generowanie opisu przez AI (Claude/Anthropic)
        [Authorize]
        [HttpPost("generate-description")]
        [SwaggerOperation(Summary = "Generate AI book description", Description = "Uses Claude (Anthropic) to generate a 2-3 sentence book description in Polish.")]
        public async Task<IActionResult> GenerateDescription([FromBody] GenerateDescriptionRequest req)
        {
            try
            {
                var description = await _aiService.GenerateBookDescriptionAsync(
                    req.Title, req.Author, req.Genre, req.Year);
                return Ok(new { description });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "AI generation failed", details = ex.Message });
            }
        }

        // Historia komend CRUD z Azure Table Storage (wzorzec Command)
        [Authorize]
        [HttpGet("history")]
        [SwaggerOperation(Summary = "Show command history", Description = "Returns all executed CRUD commands persisted in Azure Table Storage (Command pattern).")]
        public async Task<ActionResult> GetHistory()
        {
            try
            {
                var entries = await _historyStore.GetAllAsync();
                var result  = entries.Select(h => new { description = h.Description, executedAt = h.ExecutedAt });
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to load history", details = ex.Message });
            }
        }
    }
}
