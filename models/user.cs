using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using LibraryAppApi.Enums;

namespace LibraryAppApi.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        [Required]
        public UserRole Role { get; set; } = UserRole.Member; 

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}