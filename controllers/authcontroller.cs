using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraryAppApi.Data;
using LibraryAppApi.DTOs;
using LibraryAppApi.Models;
using LibraryAppApi.Services;
using LibraryAppApi.Enums;

namespace LibraryAppApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly JwtService _jwtService;

        public AuthController(ApplicationDbContext context, JwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterUserDto request)
        {
            // 1. Check if the email already exists in the database
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                return BadRequest(new { Message = "An account with this email already exists." });
            }

            // 2. Create the new user object
            var user = new User
            {
                Name = request.Name,
                Email = request.Email,
                // 3. Hash the password before saving to the database
                Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = UserRole.Admin, 
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            // 4. Save to database
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "User registered successfully." });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginUserDto request)
        {
            // 1. Find the user by their email
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

            // 2. If user doesn't exist OR the password hash doesn't match, reject them
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
            {
                return Unauthorized(new { Message = "Invalid email or password." });
            }

            // 3. Prevent deactivated users from logging in
            if (!user.IsActive)
            {
                return Unauthorized(new { Message = "Your account has been deactivated. Please contact an administrator." });
            }

            // 4. Generate the JWT Token using our injected service
            var token = _jwtService.GenerateToken(user);

            // 5. Map the User model to a safe UserDto for the frontend
            var userDto = new UserDto
            {
                UserId = user.UserId,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role.ToString(), // Convert Enum to string
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            };

            // 6. Return the token alongside the user details
            return Ok(new
            {
                Message = "Login successful.",
                Token = token,
                User = userDto
            });
        }

        // 1. GET ALL USERS
        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _context.Users
                .Select(u => new UserDto
                {
                    UserId = u.UserId,
                    Name = u.Name,
                    Email = u.Email,
                    Role = u.Role.ToString(),
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync();

            return Ok(users);
        }

        // 2. GET USER DETAILS (Includes their borrowed books)
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            var user = await _context.Users
                .Include(u => u.Transactions)
                    .ThenInclude(t => t.Book) // Pull in the book details for their transactions
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null) return NotFound(new { Message = "User not found." });

            // We construct an anonymous object here to easily return the user + their active transactions
            var userDetails = new
            {
                user.UserId,
                user.Name,
                user.Email,
                Role = user.Role.ToString(),
                user.IsActive,
                user.CreatedAt,
                ActiveTransactions = user.Transactions
                    .Where(t => t.TransactionStatus == Enums.TransactionStatus.Active || t.TransactionStatus == Enums.TransactionStatus.Overdue)
                    .Select(t => new
                    {
                        t.TransactionId,
                        t.BookId,
                        BookTitle = t.Book?.Title,
                        t.BorrowDate,
                        t.DueDate,
                        Status = t.TransactionStatus.ToString()
                    })
            };

            return Ok(userDetails);
        }

        // 3. TOGGLE USER STATUS (Suspend / Reactivate)
        [HttpPatch("{id}/toggle-status")]
        public async Task<IActionResult> ToggleUserStatus(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound(new { Message = "User not found." });

            user.IsActive = !user.IsActive;
            await _context.SaveChangesAsync();

            var statusStr = user.IsActive ? "reactivated" : "suspended";
            return Ok(new { Message = $"User account has been {statusStr}." });
        }
    }
}