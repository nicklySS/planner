using System.ComponentModel.DataAnnotations.Schema;

namespace ProductionApi.Models
{
    public class ShiftWorkLogEquipment
    {
        /* Составной ключ задаём через Fluent API в DbContext */

        public int ShiftWorkLogID { get; set; }
        public ShiftWorkLog? ShiftWorkLog { get; set; }

        public int EquipmentID { get; set; }
        public Equipment? Equipment { get; set; }
    }
}
