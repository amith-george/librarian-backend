using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraryAppApi.Data;
using LibraryAppApi.DTOs;
using LibraryAppApi.Models;

namespace LibraryAppApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Requires the user to be logged in to access anything here
    public class CategoriesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. GET ALL CATEGORIES (Available to all logged-in members)
        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _context.Categories
                .Select(c => new CategoryDto
                {
                    CategoryId = c.CategoryId,
                    Name = c.Name,
                    Description = c.Description
                })
                .OrderBy(c => c.Name)
                .ToListAsync();

            return Ok(categories);
        }

        // 2. GET CATEGORY BY ID (Available to all logged-in members)
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);

            if (category == null) 
                return NotFound(new { Message = "Category not found." });

            return Ok(new CategoryDto
            {
                CategoryId = category.CategoryId,
                Name = category.Name,
                Description = category.Description
            });
        }

        // 3. CREATE A CATEGORY (Admins and Librarians only)
        [Authorize(Roles = "Admin,Librarian")]
        [HttpPost]
        public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryDto request)
        {
            var category = new Category
            {
                Name = request.Name,
                Description = request.Description
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            // Returns a 201 Created status with the newly created category details
            return CreatedAtAction(nameof(GetCategory), new { id = category.CategoryId }, new CategoryDto
            {
                CategoryId = category.CategoryId,
                Name = category.Name,
                Description = category.Description
            });
        }

        // 4. UPDATE A CATEGORY (Admins and Librarians only)
        [Authorize(Roles = "Admin,Librarian")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] CreateCategoryDto request)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) 
                return NotFound(new { Message = "Category not found." });

            category.Name = request.Name;
            category.Description = request.Description;

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Category updated successfully." });
        }

        // 5. DELETE A CATEGORY (Admins Only)
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            // Include books to check if this category is currently in use
            var category = await _context.Categories
                .Include(c => c.Books) 
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null) 
                return NotFound(new { Message = "Category not found." });

            // Safety Guard: Prevent orphaned books
            if (category.Books.Any())
            {
                return BadRequest(new { Message = "Cannot delete this category because it contains books. Please reassign or delete the books first." });
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Category deleted successfully." });
        }
    }
}