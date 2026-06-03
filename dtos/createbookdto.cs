using System.ComponentModel.DataAnnotations;

namespace LibraryAppApi.DTOs
{
    public class CreateBookDto
    {
        [Required(ErrorMessage = "Book title is required.")]
        [MaxLength(255, ErrorMessage = "Title cannot exceed 255 characters.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Author is required.")]
        [MaxLength(255, ErrorMessage = "Author cannot exceed 255 characters.")]
        public string Author { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? ImageUrl { get; set; }

        [Required(ErrorMessage = "Total copies is required.")]
        [Range(0, int.MaxValue, ErrorMessage = "Total copies must be 0 or greater.")]
        public int TotalCopies { get; set; }

        [Required(ErrorMessage = "Category ID is required.")]
        public int CategoryId { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
