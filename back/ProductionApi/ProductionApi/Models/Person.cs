using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ProductionApi.Models
{
    public class Person
    {
        [Key]
        public int PersonID { get; set; }

        [Required]
        [MaxLength(150)]
        public string FullName { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string Role { get; set; } = null!;

        [Required]
        public bool IsActive { get; set; } = true;

        /* Навигация */

        // Ссылки на ShiftWorkLog, если человек мастер
        [JsonIgnore]
        public ICollection<ShiftWorkLog>? MasterShiftLogs { get; set; }

        // Ссылки на M:N: Наладчики в смене
        [JsonIgnore]
        public ICollection<ShiftWorkLogSetupPerson>? ShiftWorkLogSetupPeople { get; set; }
    }
}