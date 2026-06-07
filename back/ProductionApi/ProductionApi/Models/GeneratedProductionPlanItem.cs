using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProductionApi.Models
{
    public class GeneratedProductionPlanItem
    {
        [Key]
        public int ItemID { get; set; }

        [Required]
        public int GeneratedPlanID { get; set; }

        public GeneratedProductionPlan? GeneratedPlan { get; set; }

        [Required]
        [Column(TypeName = "date")]
        public DateTime WorkDate { get; set; }

        [Required]
        [MaxLength(20)]
        public string ShiftCode { get; set; } = null!;

        [Required]
        public int EquipmentID { get; set; }

        public Equipment? Equipment { get; set; }

        [Required]
        public int DetailID { get; set; }

        public Detail? Detail { get; set; }

        [Required]
        public int PlannedQuantity { get; set; }

        [Required]
        public bool IsOverdue { get; set; }

        [MaxLength(300)]
        public string? Notes { get; set; }
    }
}
