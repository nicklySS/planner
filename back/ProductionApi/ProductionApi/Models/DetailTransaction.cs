using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProductionApi.Models
{
    public class DetailTransaction
    {
        [Key]
        public int DetailTransactionID { get; set; }

        [Required]
        public int DetailID { get; set; }

        public Detail? Detail { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        [MaxLength(50)]
        public string TransactionType { get; set; } = null!;

        [Required]
        public DateTime TransactionDate { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public int? DocumentNumber { get; set; }
    }
}
