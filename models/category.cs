using System.ComponentModel.DataAnnotations;

namespace LibraryAppApi.Models
{
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        // Navigation Property
        public virtual ICollection<Book> Books { get; set; } = new List<Book>();
    }
}