using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ProductionApi.Models
{
    public class Operation
    {
        [Key]
        public int OperationID { get; set; }

        /* FK на Equipment */
        [Required]
        public int EquipmentID { get; set; }

        [JsonIgnore]
        public Equipment? Equipment { get; set; }

        /* FK на Detail */
        [Required]
        public int DetailID { get; set; }

        [JsonIgnore]
        public Detail? Detail { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int PlannedQuantity { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int CompletedQuantity { get; set; } = 0;

        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "Planned"; // Planned, InProgress, Completed и т.д.
    }
}