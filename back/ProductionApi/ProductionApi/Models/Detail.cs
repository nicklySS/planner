using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ProductionApi.Models
{
    public class Detail
    {
        [Key]
        public int DetailID { get; set; }

        [Required]
        [MaxLength(150)]
        public string DetailName { get; set; } = null!;

        [MaxLength(50)]
        public string? DetailShortCode { get; set; }

        [Column(TypeName = "decimal(18, 4)")]
        public decimal? ConsumptionRate { get; set; }

        [MaxLength(50)]
        public string? DetailCode { get; set; }

        public int? MainMaterial { get; set; }

        /* Навигация */

        [ForeignKey(nameof(MainMaterial))]
        public Material? Material { get; set; }

        // Производственные операции с этой деталью
        [JsonIgnore]
        public ICollection<Operation>? Operations { get; set; }

        // Переналадки: как FromDetail
        [JsonIgnore]
        public ICollection<DetailToDetailReconfigurationTime>? FromReconfigurations { get; set; }

        // Переналадки: как ToDetail
        [JsonIgnore]
        public ICollection<DetailToDetailReconfigurationTime>? ToReconfigurations { get; set; }
    }
}