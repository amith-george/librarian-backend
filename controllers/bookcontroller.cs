using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraryAppApi.Models;
using LibraryAppApi.DTOs;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using LibraryAppApi.Data;

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
    }
}