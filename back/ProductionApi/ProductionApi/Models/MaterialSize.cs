using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProductionApi.Models
{
    public class MaterialSize
    {
        [Key]
        public int MaterialSizeID { get; set; }

        [Required]
        public decimal SizeValue { get; set; }

        [Required]
        [MaxLength(20)]
        public string Unit { get; set; } = null!; // кг, м, см и т.д.

        /* Навигация */

        // M:N: материалы с этой размерностью
        public ICollection<MaterialMaterialSize>? MaterialMaterialSizes { get; set; }
    }
}
