using System.ComponentModel.DataAnnotations;

namespace LibraryAppApi.DTOs
{
    public class CreateCategoryDto
    {
        [Required(ErrorMessage = "Category name is required.")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }
    }
}