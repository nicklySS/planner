using System.ComponentModel.DataAnnotations.Schema;

namespace ProductionApi.Models
{
    public class ShiftWorkLogSetupPerson
    {
        /* Составной ключ задаём через Fluent API в DbContext */

        public int ShiftWorkLogID { get; set; }
        public ShiftWorkLog? ShiftWorkLog { get; set; }

        public int PersonID { get; set; }
        public Person? Person { get; set; }
    }
}
