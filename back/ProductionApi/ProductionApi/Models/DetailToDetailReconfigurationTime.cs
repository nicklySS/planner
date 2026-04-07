using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProductionApi.Models
{
    public class DetailToDetailReconfigurationTime
    {
        [Key]
        public int ReconfigurationID { get; set; }

        /* FK на станок */
        [Required]
        public int EquipmentID { get; set; }
        public Equipment? Equipment { get; set; }

        /* FK на From Detail */
        [Required]
        public int FromDetailID { get; set; }
        [ForeignKey("FromDetailID")]
        public Detail? FromDetail { get; set; }

        /* FK на To Detail */
        [Required]
        public int ToDetailID { get; set; }
        [ForeignKey("ToDetailID")]
        public Detail? ToDetail { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int ReconfigurationMinutes { get; set; }

        [MaxLength(300)]
        public string? Notes { get; set; }
    }
}
