using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ProductionApi.Models
{
    public class TimeSheet
    {
        [Key]
        public int TimeSheetID { get; set; }

        [Required]
        public int PersonID { get; set; }

        [Required]
        public DateTime WorkDate { get; set; }

        [Required]
        [MaxLength(20)]
        public string ShiftCode { get; set; } = null!;

        [Range(0, 24)]
        public decimal? HoursWorked { get; set; }

        [Required]
        [MaxLength(20)]
        public string DayType { get; set; } = null!; // 'Work', 'DayOff', 'Holiday', 'Sick'

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /* Навигация */

        [ForeignKey("PersonID")]
        [JsonIgnore]
        public virtual Person? Person { get; set; }
    }
}
