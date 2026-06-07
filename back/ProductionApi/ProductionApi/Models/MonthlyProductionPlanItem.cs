using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProductionApi.Models
{
    public class MonthlyProductionPlanItem
    {
        [Key]
        public int PlanItemID { get; set; }

        [Required]
        public int PlanID { get; set; }

        public MonthlyProductionPlan? Plan { get; set; }

        [Required]
        public int DetailID { get; set; }

        public Detail? Detail { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        [Column(TypeName = "date")]
        public DateTime ShipmentDate { get; set; }

        [MaxLength(300)]
        public string? Notes { get; set; }
    }
}
