using System;

namespace LibraryAppApi.DTOs
{
    public class TransactionDto
    {
        public int TransactionId { get; set; }
        
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty; 
        
        public int BookId { get; set; }
        public string BookTitle { get; set; } = string.Empty; 
        
        public DateTime BorrowDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        
        // Convert the Enum to a readable string for the frontend
        public string TransactionStatus { get; set; } = string.Empty; 
    }
}