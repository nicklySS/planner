using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ProductionApi.Models
{
    public class EquipmentTimeSheet
    {
        [Key]
        public int EquipmentTimeSheetID { get; set; }

        [Required]
        public int EquipmentID { get; set; }

        [Required]
        public DateTime WorkDate { get; set; }

        [Required]
        [MaxLength(20)]
        public string ShiftCode { get; set; } = null!;

        [Range(0, 24)]
        public decimal? HoursWorked { get; set; }

        [Required]
        [MaxLength(20)]
        public string DayType { get; set; } = null!; // 'Work', 'DayOff', 'Repair'

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ModifiedAt { get; set; }

        /* Навигация */

        [ForeignKey("EquipmentID")]
        [JsonIgnore]
        public virtual Equipment? Equipment { get; set; }
    }
}
