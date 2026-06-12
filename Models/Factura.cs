using System.ComponentModel.DataAnnotations;

namespace APIRESTful.Models
{
    public class Factura
    {
        [Key] public int factura_id { get; set; }
        public int reservacion_id { get; set; }   // FK a app.Reservaciones
        public decimal tipo_cambio_usd { get; set; }
        public decimal total_usd { get; set; }
        public DateTime emitida_en { get; set; } = DateTime.UtcNow;

        public Reservacion reservacion { get; set; }
    }
}
