using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API_Security.Models
{
    [Table("Roles", Schema = "sec")]
    public class Rol
    {
        [Key]
        public int rol_id { get; set; }

        [Required]
        [MaxLength(40)]
        public string nombre { get; set; }
    }

}
