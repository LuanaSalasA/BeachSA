
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API_Security.Models
{
    [Table("Auditoria", Schema = "sec")]
    public class Auditoria
    {
        [Key]
        public long auditoria_id { get; set; }

        public int? usuario_id { get; set; }

        public DateTime fecha { get; set; }

        [MaxLength(1000)]
        public string? detalles { get; set; }

    }
}