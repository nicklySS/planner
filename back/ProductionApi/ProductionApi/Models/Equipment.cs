using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ProductionApi.Models
{
    public class Equipment
    {
        [Key]
        public int EquipmentID { get; set; }

        [Required]
        [MaxLength(150)]
        public string EquipmentName { get; set; } = null!;

        [MaxLength(100)]
        public string? EquipmentType { get; set; }

        /* FK на рабочее место */
        public int? WorkPlaceID { get; set; }

        [JsonIgnore]
        public WorkPlace? WorkPlace { get; set; }

        /* Навигация */

        // Переналадки
        [JsonIgnore]
        public ICollection<DetailToDetailReconfigurationTime>? ReconfigurationTimes { get; set; }

        // Производственные операции
        [JsonIgnore]
        public ICollection<Operation>? Operations { get; set; }

        // M:N: Станки в сменах
        [JsonIgnore]
        public ICollection<ShiftWorkLogEquipment>? ShiftWorkLogs { get; set; }
    }
}