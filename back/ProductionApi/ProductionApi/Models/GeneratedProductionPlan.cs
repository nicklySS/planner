using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProductionApi.Models
{
    public class GeneratedProductionPlan
    {
        [Key]
        public int GeneratedPlanID { get; set; }

        [Required]
        public int Year { get; set; }

        [Required]
        public int Month { get; set; }

        [Required]
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(500)]
        public string? Notes { get; set; }

        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "Draft";

        public DateTime? ConfirmedAt { get; set; }

        public ICollection<GeneratedProductionPlanItem>? Items { get; set; }
    }
}
