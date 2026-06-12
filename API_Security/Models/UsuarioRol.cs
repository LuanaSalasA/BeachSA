using System.ComponentModel.DataAnnotations.Schema;

namespace API_Security.Models
{
    [Table("UsuarioRol", Schema = "sec")]
    public class UsuarioRol
    {
        public int usuario_id { get; set; }
        public int rol_id { get; set; }

        [ForeignKey("usuario_id")]
        public Usuario Usuario { get; set; }

        [ForeignKey("rol_id")]
        public Rol Rol { get; set; }

       
    }
}