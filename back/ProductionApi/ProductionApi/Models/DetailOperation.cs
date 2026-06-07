using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ProductionApi.Models
{
    public class DetailOperation
    {
        [Key]
        public int DetailOperationID { get; set; }

        /* FK на Detail */
        [Required]
        public int DetailID { get; set; }

        [JsonIgnore]
        public Detail? Detail { get; set; }

        /* FK на Equipment */
        [Required]
        public int EquipmentID { get; set; }

        public Equipment? Equipment { get; set; }

        /* Номер в порядке выполнения */
        public int? SequenceNumber { get; set; }

        [MaxLength(50)]
        public string? OperationCode { get; set; }

        [MaxLength(50)]
        public string? OperationType { get; set; }

        /* Время переналадки в минутах */
        [Range(0, int.MaxValue)]
        public int? ReconfigurationTime { get; set; }

        /* Процент на наладку */
        [Column(TypeName = "decimal(5, 2)")]
        public decimal? SetupPercentage { get; set; }

        /* Норма выработки за смену (шт.) */
        [Range(0, int.MaxValue)]
        public int? NormPerShift { get; set; }
    }
}
