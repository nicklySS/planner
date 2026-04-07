using System.ComponentModel.DataAnnotations.Schema;

namespace ProductionApi.Models
{
    public class MaterialMaterialSize
    {
        /* Составной ключ задаём через Fluent API в DbContext */

        public int MaterialID { get; set; }
        public Material? Material { get; set; }

        public int MaterialSizeID { get; set; }
        public MaterialSize? MaterialSize { get; set; }
    }
}
