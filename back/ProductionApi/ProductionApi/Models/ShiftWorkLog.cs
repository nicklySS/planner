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

        /* FK на мастера, который выдал задание */
        [Required]
        public int MasterID { get; set; }
        public Person? Master { get; set; }

        /* FK на рабочего, которому выдано задание */
        public int? WorkerID { get; set; }
        public Person? Worker { get; set; }

        /* FK на деталь, которую нужно сделать */
        public int? DetailID { get; set; }
        public Detail? Detail { get; set; }

        /* Количество деталей */
        public int? Quantity { get; set; }

        /* FK на материал */
        public int? MaterialID { get; set; }
        public Material? Material { get; set; }

        [MaxLength(300)]
        public string? Notes { get; set; }

        /* Навигация */

        // M:N: Наладчики в смене (если нужны помощники)
        public ICollection<ShiftWorkLogSetupPerson>? SetupPeople { get; set; }

        // M:N: Станки в смене (если нужно отслеживать оборудование)
        public ICollection<ShiftWorkLogEquipment>? Equipments { get; set; }
    }
}
