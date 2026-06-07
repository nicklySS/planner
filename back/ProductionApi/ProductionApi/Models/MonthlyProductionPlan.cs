using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProductionApi.Models
{
    public class MonthlyProductionPlan
    {
        [Key]
        public int PlanID { get; set; }

        [Required]
        public int Year { get; set; }

        [Required]
        public int Month { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<MonthlyProductionPlanItem>? Items { get; set; }
    }
}
