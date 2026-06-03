using System.ComponentModel.DataAnnotations;
using LibraryAppApi.Enums;

namespace LibraryAppApi.DTOs
{
    public class UpdateTransactionStatusDto
    {
        [Required(ErrorMessage = "Transaction status is required.")]
        public TransactionStatus Status { get; set; }
    }
}