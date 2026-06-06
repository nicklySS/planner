using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ProductionApi.Models
{
    public class WorkPlace
    {
        [Key]
        public int WorkPlaceID { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = null!;

        [MaxLength(200)]
        public string? Location { get; set; }

        [MaxLength(300)]
        public string? Notes { get; set; }

        /* Навигация */

        // 1:1 relationship with Person (обратная навигация)
        [JsonIgnore]
        public Person? ResponsiblePerson { get; set; }

        // Станки на этом рабочем месте
        [JsonIgnore]
        public ICollection<Equipment>? Equipments { get; set; }
    }
}