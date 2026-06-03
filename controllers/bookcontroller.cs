using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraryAppApi.Models;
using LibraryAppApi.DTOs;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using LibraryAppApi.Data;
using Microsoft.AspNetCore.Authorization;
using LibraryAppApi.Enums;

namespace LibraryAppApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BooksController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BooksController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResultDto<BookDto>>> GetAllBooks([FromQuery] BookQueryParametersDto queryParams)
        {

            var query = _context.Books.Include(b => b.Category).AsQueryable();

            if (queryParams.CategoryId.HasValue && queryParams.CategoryId.Value > 0)
            {
                query = query.Where(b => b.CategoryId == queryParams.CategoryId.Value);
            }

            if (queryParams.IsActive.HasValue)
            {
                query = query.Where(b => b.IsActive == queryParams.IsActive.Value);
            }

            if (queryParams.OnlyAvailable)
            {
                query = query.Where(b => b.AvailableCopies > 0);
            }

            var totalCount = await query.CountAsync();

            var books = await query
                .OrderBy(b => b.Title)
                .Skip((queryParams.PageNumber - 1) * queryParams.PageSize)
                .Take(queryParams.PageSize)
                .Select(b => new BookDto
                {
                    BookId = b.BookId,
                    Title = b.Title,
                    Author = b.Author,
                    Description = b.Description,
                    ImageUrl = b.ImageUrl,
                    TotalCopies = b.TotalCopies,
                    AvailableCopies = b.AvailableCopies,
                    IsActive = b.IsActive,
                    CategoryId = b.CategoryId,
                    CategoryName = b.Category != null ? b.Category.Name : string.Empty // Include Category details
                })
                .ToListAsync();

            return Ok(new PagedResultDto<BookDto>
            {
                Items = books,
                TotalCount = totalCount,
                PageNumber = queryParams.PageNumber,
                PageSize = queryParams.PageSize
            });
        }

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<BookDto>>> SearchBooks([FromQuery] string term)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                return Ok(new List<BookDto>());
            }

            var lowerTerm = term.ToLower();

            var books = await _context.Books
                .Include(b => b.Category)
                .Where(b => b.Title.ToLower().Contains(lowerTerm) || b.Author.ToLower().Contains(lowerTerm))
                .OrderByDescending(b => b.Title.ToLower().StartsWith(lowerTerm)) 
                .ThenBy(b => b.Title)
                .Take(13) // Limit top 13
                .Select(b => new BookDto
                {
                    BookId = b.BookId,
                    Title = b.Title,
                    Author = b.Author,
                    Description = b.Description,
                    ImageUrl = b.ImageUrl,
                    TotalCopies = b.TotalCopies,
                    AvailableCopies = b.AvailableCopies,
                    IsActive = b.IsActive,
                    CategoryId = b.CategoryId,
                    CategoryName = b.Category != null ? b.Category.Name : string.Empty
                })
                .ToListAsync();

            return Ok(books);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BookDto>> GetBook(int id)
        {
            var book = await _context.Books
                .Include(b => b.Category)
                .Where(b => b.BookId == id)
                .Select(b => new BookDto
                {
                    BookId = b.BookId,
                    Title = b.Title,
                    Author = b.Author,
                    Description = b.Description,
                    ImageUrl = b.ImageUrl,
                    TotalCopies = b.TotalCopies,
                    AvailableCopies = b.AvailableCopies,
                    IsActive = b.IsActive,
                    CategoryId = b.CategoryId,
                    CategoryName = b.Category != null ? b.Category.Name : string.Empty
                })
                .FirstOrDefaultAsync();

            if (book == null)
            {
                return NotFound(new { message = "Book not found." });
            }

            return Ok(book);
        }

        [Authorize(Roles = "Admin,Librarian")]
        [HttpPost]
        public async Task<IActionResult> CreateBook([FromBody] CreateBookDto request)
        {
            var categoryExists = await _context.Categories.AnyAsync(c => c.CategoryId == request.CategoryId);
            if (!categoryExists)
            {
                return BadRequest(new { Message = "Invalid Category ID." });
            }

            var book = new Book
            {
                Title = request.Title,
                Author = request.Author,
                Description = request.Description,
                ImageUrl = request.ImageUrl,
                TotalCopies = request.TotalCopies,
                AvailableCopies = request.TotalCopies, // All copies are initially available
                IsActive = request.IsActive,
                CategoryId = request.CategoryId
            };

            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            var categoryName = await _context.Categories
                .Where(c => c.CategoryId == book.CategoryId)
                .Select(c => c.Name)
                .FirstOrDefaultAsync() ?? string.Empty;

            return CreatedAtAction(nameof(GetBook), new { id = book.BookId }, new BookDto
            {
                BookId = book.BookId,
                Title = book.Title,
                Author = book.Author,
                Description = book.Description,
                ImageUrl = book.ImageUrl,
                TotalCopies = book.TotalCopies,
                AvailableCopies = book.AvailableCopies,
                IsActive = book.IsActive,
                CategoryId = book.CategoryId,
                CategoryName = categoryName
            });
        }

        [Authorize(Roles = "Admin,Librarian")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBook(int id, [FromBody] CreateBookDto request)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null)
            {
                return NotFound(new { Message = "Book not found." });
            }

            var categoryExists = await _context.Categories.AnyAsync(c => c.CategoryId == request.CategoryId);
            if (!categoryExists)
            {
                return BadRequest(new { Message = "Invalid Category ID." });
            }

            // Calculate quantity adjustments
            int currentBorrowed = book.TotalCopies - book.AvailableCopies;
            if (request.TotalCopies < currentBorrowed)
            {
                return BadRequest(new { Message = $"Total copies cannot be less than the number of currently borrowed copies (borrowed: {currentBorrowed})." });
            }

            book.Title = request.Title;
            book.Author = request.Author;
            book.Description = request.Description;
            book.ImageUrl = request.ImageUrl;
            book.AvailableCopies = request.TotalCopies - currentBorrowed;
            book.TotalCopies = request.TotalCopies;
            book.IsActive = request.IsActive;
            book.CategoryId = request.CategoryId;

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Book updated successfully." });
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBook(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null)
            {
                return NotFound(new { Message = "Book not found." });
            }

            // Safety Guard: Check for active or overdue transactions
            var hasActiveTransactions = await _context.Transactions.AnyAsync(t => t.BookId == id && 
                (t.TransactionStatus == TransactionStatus.Active || t.TransactionStatus == TransactionStatus.Overdue));

            if (hasActiveTransactions)
            {
                return BadRequest(new { Message = "Cannot delete this book because it is currently borrowed." });
            }

            _context.Books.Remove(book);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Book deleted successfully." });
        }
    }
}