using System.ComponentModel.DataAnnotations;

namespace APIRESTful.Models
{
    public class Cliente
    {
        [Key]
        public string cedula { get; set; }
        public string tipo_cedula { get; set; }
        public string fullname { get; set; }
        public string telefono { get; set; }
        public string direccion { get; set; }
        public string email { get; set; }
    }
}
