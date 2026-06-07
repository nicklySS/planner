using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProductionApi.Models
{
    public class DetailStock
    {
        [Key]
        public int DetailStockID { get; set; }

        [Required]
        public int DetailID { get; set; }

        public Detail? Detail { get; set; }

        [Required]
        public int CurrentQuantity { get; set; }

        [Required]
        public int ReceivedQuantity { get; set; }

        [Required]
        public int ShippedQuantity { get; set; }

        [Required]
        public DateTime LastUpdated { get; set; }
    }
}
