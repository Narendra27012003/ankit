using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebApiTemplate.DTO;
using WebApiTemplate.Services;
using FluentValidation;
using FluentValidation.Results;

namespace WebApiTemplate.Controllers
{
    [Route("api/books")]
    [ApiController]
    public class BookController : ControllerBase
    {
        private readonly BookService _bookService;
        private readonly IValidator<BookFilterDto> _filterValidator;
        private readonly IValidator<BookDto> _bookValidator;

        public BookController(BookService bookService, IValidator<BookFilterDto> filterValidator, IValidator<BookDto> bookValidator)
        {
            _bookService = bookService;
            _filterValidator = filterValidator;
            _bookValidator = bookValidator;
        }

        // ✅ Get books with Pagination, Filtering & DSQL Query
        [HttpGet]
        [AllowAnonymous]
        public IActionResult GetBooks([FromQuery] BookFilterDto filter)
        {
            ValidationResult validationResult = _filterValidator.Validate(filter);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors.Select(e => e.ErrorMessage));
            }

            var books = _bookService.GetBooks(filter);
            return Ok(books);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public IActionResult GetBook(int id)
        {
            var book = _bookService.GetBookById(id);
            if (book == null) return NotFound(new { message = "Book not found" });
            return Ok(book);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Author")]
        public IActionResult AddBook([FromBody] BookDto bookDto)
        {
            var username = User.FindFirstValue(ClaimTypes.Name);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            ValidationResult validationResult = _bookValidator.Validate(bookDto);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors.Select(e => e.ErrorMessage));
            }

            if (userRole == "Author")
            {
                _bookService.AddBook(bookDto, username);
            }
            else if (userRole == "Admin")
            {
                _bookService.AddBook(bookDto);
            }
            else
            {
                return Forbid();
            }

            return Created("", new { message = "Book added successfully" });
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Author")]
        public IActionResult UpdateBook(int id, [FromBody] BookDto bookDto)
        {
            var username = User.FindFirstValue(ClaimTypes.Name);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            ValidationResult validationResult = _bookValidator.Validate(bookDto);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors.Select(e => e.ErrorMessage));
            }

            if (userRole == "Admin" || (userRole == "Author" && _bookService.IsBookOwner(id, username)))
            {
                bool updated = _bookService.UpdateBook(id, bookDto);
                if (!updated) return NotFound(new { message = "Book not found" });

                return Ok(new { message = "Book updated successfully" });
            }

            return Forbid();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Author")]
        public IActionResult DeleteBook(int id)
        {
            var username = User.FindFirstValue(ClaimTypes.Name);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            if (userRole == "Admin" || (userRole == "Author" && _bookService.IsBookOwner(id, username)))
            {
                bool deleted = _bookService.DeleteBook(id);
                if (!deleted) return NotFound(new { message = "Book not found" });

                return Ok(new { message = "Book deleted successfully" });
            }

            return Forbid();
        }
    }
}
