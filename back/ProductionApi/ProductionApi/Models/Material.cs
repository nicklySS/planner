using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ProductionApi.Models
{
    public class Material
    {
        [Key]
        public int MaterialID { get; set; }

        [Required]
        [MaxLength(150)]
        public string MaterialName { get; set; } = null!;

        /* Навигация */

        // M:N: размеры материала
        public ICollection<MaterialMaterialSize>? MaterialMaterialSizes { get; set; }

        // 1:N: детали, использующие этот материал как основной
        [JsonIgnore]
        public ICollection<Detail>? Details { get; set; }
    }
}
