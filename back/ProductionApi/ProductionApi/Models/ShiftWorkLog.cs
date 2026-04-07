using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProductionApi.Models
{
    public class ShiftWorkLog
    {
        [Key]
        public int ShiftWorkLogID { get; set; }

        [Required]
        public DateTime WorkDate { get; set; }

        [Required]
        [Range(1, 3)]
        public int ShiftNumber { get; set; } // 1, 2, 3

        /* FK на мастера */
        [Required]
        public int MasterID { get; set; }
        public Person? Master { get; set; }

        [MaxLength(300)]
        public string? Notes { get; set; }

        /* Навигация */

        // M:N: Наладчики в смене
        public ICollection<ShiftWorkLogSetupPerson>? SetupPeople { get; set; }

        // M:N: Станки в смене
        public ICollection<ShiftWorkLogEquipment>? Equipments { get; set; }
    }
}
