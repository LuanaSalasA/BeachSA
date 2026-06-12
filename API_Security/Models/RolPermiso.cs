using System.ComponentModel.DataAnnotations.Schema;

namespace API_Security.Models
{
    [Table("RolPermiso", Schema = "sec")]
    public class RolPermiso
    {
        public int rol_id { get; set; }
        public int permiso_id { get; set; }

        public Rol Rol { get; set; }
        public Permiso Permiso { get; set; }
    }
}
