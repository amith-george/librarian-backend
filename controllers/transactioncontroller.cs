using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraryAppApi.Data;
using LibraryAppApi.DTOs;
using LibraryAppApi.Models;
using LibraryAppApi.Enums;

namespace LibraryAppApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Requires the user to be logged in
    public class TransactionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TransactionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. BORROW A BOOK (Members & Admins)
        [HttpPost("borrow")]
        public async Task<IActionResult> BorrowBook([FromBody] BorrowBookDto request)
        {
            // Find the book
            var book = await _context.Books.FindAsync(request.BookId);
            if (book == null || !book.IsActive)
                return NotFound(new { Message = "Book not found or is inactive." });

            // Check inventory
            if (book.AvailableCopies <= 0)
                return BadRequest(new { Message = "Sorry, there are no available copies of this book right now." });

            // Verify user exists and is active
            var user = await _context.Users.FindAsync(request.UserId);
            if (user == null || !user.IsActive)
                return BadRequest(new { Message = "Invalid or inactive user account." });

            // Create the transaction
            var transaction = new Transaction
            {
                UserId = request.UserId,
                BookId = request.BookId,
                BorrowDate = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(14), // Standard 2-week checkout period
                TransactionStatus = TransactionStatus.Active
            };

            // Decrement available copies
            book.AvailableCopies -= 1;

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Book borrowed successfully!", TransactionId = transaction.TransactionId, DueDate = transaction.DueDate });
        }

        // 2. UPDATE TRANSACTION STATUS (Admins / Librarians)
        [Authorize(Roles = "Admin,Librarian")]
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateTransactionStatusDto request)
        {
            var transaction = await _context.Transactions
                .Include(t => t.Book)
                .FirstOrDefaultAsync(t => t.TransactionId == id);

            if (transaction == null)
                return NotFound(new { Message = "Transaction not found." });

            // If the book is being marked as Returned or Lost from an Active/Overdue state
            if ((request.Status == TransactionStatus.Returned || request.Status == TransactionStatus.Lost) 
                && (transaction.TransactionStatus == TransactionStatus.Active || transaction.TransactionStatus == TransactionStatus.Overdue))
            {
                transaction.ReturnDate = DateTime.UtcNow;

                // Only increment available copies if it was safely returned (not lost)
                if (request.Status == TransactionStatus.Returned && transaction.Book != null)
                {
                    transaction.Book.AvailableCopies += 1;
                }
            }

            transaction.TransactionStatus = request.Status;
            await _context.SaveChangesAsync();

            return Ok(new { Message = $"Transaction status updated to {request.Status}." });
        }

        // 3. GET ALL TRANSACTIONS (Admins / Librarians)
        [Authorize(Roles = "Admin,Librarian")]
        [HttpGet]
        public async Task<IActionResult> GetAllTransactions()
        {
            var transactions = await _context.Transactions
                .Include(t => t.User)
                .Include(t => t.Book)
                .OrderByDescending(t => t.BorrowDate)
                .Select(t => new TransactionDto
                {
                    TransactionId = t.TransactionId,
                    UserId = t.UserId,
                    UserName = t.User != null ? t.User.Name : "Unknown",
                    BookId = t.BookId,
                    BookTitle = t.Book != null ? t.Book.Title : "Unknown",
                    BorrowDate = t.BorrowDate,
                    DueDate = t.DueDate,
                    ReturnDate = t.ReturnDate,
                    TransactionStatus = t.TransactionStatus.ToString()
                })
                .ToListAsync();

            return Ok(transactions);
        }

        // 4. GET A USER'S TRANSACTIONS (Members looking at their own history)
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserTransactions(int userId)
        {
            // Security check: Ensure the person making the request is either an Admin or requesting their own data
            var currentUserIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var currentUserRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            if (currentUserIdStr != userId.ToString() && currentUserRole != "Admin")
            {
                return Forbid(); // Returns a 403 Forbidden
            }

            var transactions = await _context.Transactions
                .Include(t => t.Book)
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.BorrowDate)
                .Select(t => new TransactionDto
                {
                    TransactionId = t.TransactionId,
                    UserId = t.UserId,
                    UserName = string.Empty, // Not needed since they know who they are
                    BookId = t.BookId,
                    BookTitle = t.Book != null ? t.Book.Title : "Unknown",
                    BorrowDate = t.BorrowDate,
                    DueDate = t.DueDate,
                    ReturnDate = t.ReturnDate,
                    TransactionStatus = t.TransactionStatus.ToString()
                })
                .ToListAsync();

            return Ok(transactions);
        }
    }
}