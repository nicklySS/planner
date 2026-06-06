using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ProductionApi.Models
{
    public class PersonRole
    {
        [Key]
        public int PersonRoleID { get; set; }

        public int PersonID { get; set; }
        public int RoleID { get; set; }

        /* Навигация */
        [JsonIgnore]
        public Person? Person { get; set; }
        public Role? Role { get; set; }
    }
}
