using System.ComponentModel.DataAnnotations;

namespace LibraryAppApi.DTOs
{
    public class BorrowBookDto
    {
        [Required(ErrorMessage = "User ID is required.")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Book ID is required.")]
        public int BookId { get; set; }
    }
}