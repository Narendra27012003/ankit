using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using WebApiTemplate.Data;
using WebApiTemplate.DTO;
using WebApiTemplate.Models;

namespace WebApiTemplate.Services
{
    public class BookService
    {
        private readonly ApplicationDbContext _context;

        public BookService(ApplicationDbContext context)
        {
            _context = context;
        }
        public IEnumerable<Book> GetBooks(BookFilterDto filter)
        {
            var query = _context.Books.AsQueryable();

            // ✅ Apply Dynamic Query (DSQL)
            if (!string.IsNullOrEmpty(filter.Dsql))
            {
                try
                {
                    string linqExpression = ParseDsql(filter.Dsql);
                    query = query.Where(linqExpression); // ✅ Apply dynamic LINQ query
                }
                catch (Exception ex)
                {
                    throw new Exception($"Invalid DSQL query format: {ex.Message}");
                }
            }

            // ✅ Apply Additional Filters
            if (!string.IsNullOrEmpty(filter.Title))
                query = query.Where(b => EF.Functions.Like(b.Title.ToLower(), $"%{filter.Title.ToLower()}%"));
            if (!string.IsNullOrEmpty(filter.Author))
                query = query.Where(b => EF.Functions.Like(b.Author.ToLower(), $"%{filter.Author.ToLower()}%"));
            if (!string.IsNullOrEmpty(filter.Publisher))
                query = query.Where(b => EF.Functions.Like(b.Publisher.ToLower(), $"%{filter.Publisher.ToLower()}%"));
            if (filter.PublicationYear.HasValue)
                query = query.Where(b => b.PublicationYear == filter.PublicationYear.Value);

            // ✅ Apply Pagination
            int page = filter.Page > 0 ? filter.Page : 1;
            int limit = filter.Limit > 0 && filter.Limit <= 100 ? filter.Limit : 10;

            return query
                .OrderBy(b => b.Id)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToList();
        }

        private string ParseDsql(string dsql)
        {
            // Ensure spacing around operators for valid expressions
            dsql = dsql.Replace(" AND ", " && ")
                       .Replace(" OR ", " || ")
                       .Replace(">=", " >= ") // Ensure spacing for greater than or equal
                       .Replace("<=", " <= ") // Ensure spacing for less than or equal
                       .Replace("!=", " != ") // Ensure spacing for inequality
                       .Replace(">", " > ") // Ensure spacing for greater than
                       .Replace("<", " < ") // Ensure spacing for less than
                       .Replace(" in [", ".Contains(new[]") // Convert "in" to LINQ Contains
                       .Replace("]", ")"); // Closing for Contains

            // Ensure proper handling of string values inside quotes
            dsql = dsql.Replace("\"", "'"); // Convert double quotes to single quotes for LINQ compatibility

            return dsql;
        }






        // ✅ Get a single book by ID
        public Book? GetBookById(int id) => _context.Books.FirstOrDefault(b => b.Id == id);

        // ✅ Add a book (for Admin or Author)
        public void AddBook(BookDto bookDto, string? createdBy = null)
        {
            var book = new Book
            {
                Title = bookDto.Title,
                Author = bookDto.Author,
                Description = bookDto.Description,
                Genre = bookDto.Genre ?? new List<string>(), // Ensuring a valid list
                Publisher = bookDto.Publisher,
                PublicationYear = bookDto.PublicationYear
            };

            _context.Books.Add(book);
            _context.SaveChanges();
        }

        // ✅ Update a book (for Admin or book owner)
        public bool UpdateBook(int id, BookDto bookDto)
        {
            var book = _context.Books.FirstOrDefault(b => b.Id == id);
            if (book == null) return false;

            book.Title = bookDto.Title;
            book.Author = bookDto.Author;
            book.Description = bookDto.Description;
            book.Genre = bookDto.Genre ?? new List<string>(); // Ensuring a valid list
            book.Publisher = bookDto.Publisher;
            book.PublicationYear = bookDto.PublicationYear;

            _context.SaveChanges();
            return true;
        }

        // ✅ Check if a user is the book's owner
        public bool IsBookOwner(int bookId, string username)
        {
            var book = _context.Books.FirstOrDefault(b => b.Id == bookId);
            return book != null && book.Author == username;
        }

        // ✅ Delete a book (for Admin or book owner)
        public bool DeleteBook(int id)
        {
            var book = _context.Books.FirstOrDefault(b => b.Id == id);
            if (book == null) return false;

            _context.Books.Remove(book);
            _context.SaveChanges();
            return true;
        }
    }
}
