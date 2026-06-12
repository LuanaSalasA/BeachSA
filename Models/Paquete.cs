using System.ComponentModel.DataAnnotations;

namespace APIRESTful.Models
{ 
    public class Paquete
    {
        [Key]
        public int paquete_id { get; set; }
        public string nombre { get; set; }
        public decimal costo_por_persona_noche { get; set; }
        public decimal prima_porcentaje { get; set; }
        public int mensualidades { get; set; }
    }
}
