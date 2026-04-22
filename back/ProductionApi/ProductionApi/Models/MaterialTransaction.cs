using System;
using System.ComponentModel.DataAnnotations;

namespace ProductionApi.Models
{
    public class MaterialTransaction
    {
        [Key]
        public int TransactionID { get; set; }

        [Required]
        public int MaterialID { get; set; }
        public Material? Material { get; set; }

        [Required]
        public int MaterialSizeID { get; set; }
        public MaterialSize? MaterialSize { get; set; }

        [Required]
        public decimal Quantity { get; set; }  // количество (положительное для прихода, отрицательное для расхода)

        [Required]
        [MaxLength(50)]
        public string TransactionType { get; set; } = null!;  // "Receipt" или "Consumption"

        [Required]
        public DateTime TransactionDate { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }  // примечание

        public int? DocumentNumber { get; set; }  // номер документа
    }
}
