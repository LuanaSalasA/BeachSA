using System.ComponentModel.DataAnnotations;

namespace APIRESTful.Models
{
    public class Reservacion
    {
        [Key] public int reservacion_id { get; set; }

        [Required] 
        public string cliente_cedula { get; set; }
        public Cliente cliente { get; set; }

        [Required] 
        public int paquete_id { get; set; }
        public Paquete? paquete { get; set; }

        [Required] 
        public int metodo_pago_id { get; set; }
        public TipoPago? tipo_pago { get; set; }

        [Required] 
        public int noches { get; set; }

        [Required] 
        public int personas { get; set; }

        public string? numero_cheque { get; set; }
        public string? banco_cheque { get; set; }

        public decimal descuento_pct { get; set; }
        public decimal subtotal_colones { get; set; }
        public decimal descuento_colones { get; set; }
        public decimal iva_pct { get; set; }
        public decimal iva_colones { get; set; }
        public decimal total_colones { get; set; }

        // CALCULADOS: no existen en la tabla app.Reservaciones (EF los ignora en OnModelCreating)
        public decimal tipo_cambio_usd { get; set; }
        public decimal total_usd { get; set; }

        public decimal prima_porcentaje { get; set; }
        public decimal prima_monto { get; set; }
        public int mensualidades { get; set; }
        public DateTime fecha_reservacion { get; set; } = DateTime.Now;
    }
}
