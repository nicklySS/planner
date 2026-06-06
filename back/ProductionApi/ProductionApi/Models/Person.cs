using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ProductionApi.Models
{
    public class Person
    {
        [Key]
        public int PersonID { get; set; }

        [MaxLength(50)]
        public string? EmployeeNumber { get; set; }

        [MaxLength(500)]
        [JsonIgnore]
        public string? PasswordHash { get; set; }

        [MaxLength(150)]
        public string? FullName { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        // Foreign Key to WorkPlace (1:1 relationship)
        public int? WorkPlaceID { get; set; }

        /* Навигация */

        // 1:1 relationship with WorkPlace
        public WorkPlace? WorkPlace { get; set; }

        // M:N relationship with Roles
        public ICollection<PersonRole>? PersonRoles { get; set; }

        // Ссылки на ShiftWorkLog, если человек мастер
        [JsonIgnore]
        public ICollection<ShiftWorkLog>? MasterShiftLogs { get; set; }

        // Ссылки на M:N: Наладчики в смене
        [JsonIgnore]
        public ICollection<ShiftWorkLogSetupPerson>? ShiftWorkLogSetupPeople { get; set; }
    }
}