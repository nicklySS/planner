using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProductionApi.Models
{
    public class MaterialStock
    {
        [Key]
        public int MaterialStockID { get; set; }

        [Required]
        public int MaterialID { get; set; }
        public Material? Material { get; set; }

        [Required]
        public int MaterialSizeID { get; set; }
        public MaterialSize? MaterialSize { get; set; }

        [Required]
        public decimal CurrentQuantity { get; set; }  // остаток в штуках (заготовок данной размерности)

        [Required]
        public decimal ReceivedQuantity { get; set; }  // всего получено

        [Required]
        public decimal UsedQuantity { get; set; }  // всего использовано

        [Required]
        public DateTime LastUpdated { get; set; }
    }
}
