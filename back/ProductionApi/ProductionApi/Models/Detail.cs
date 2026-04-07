using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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