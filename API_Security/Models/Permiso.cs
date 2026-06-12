using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API_Security.Models
{
    [Table("Permisos", Schema = "sec")]
    public class Permiso
    {
        [Key]
        public int permiso_id { get; set; }

        [Required]
        [MaxLength(60)]
        public string recurso { get; set; }

        [Required]
        [MaxLength(20)]
        public string accion { get; set; }
    }

}
