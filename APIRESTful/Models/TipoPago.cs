using System.ComponentModel.DataAnnotations;

namespace APIRESTful.Models
{
    public class TipoPago
    {
        [Key]
        public int tipo_pago_id { get; set; }
        public string nombre { get; set; }
    }
}
