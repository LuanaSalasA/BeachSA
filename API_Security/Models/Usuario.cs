using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API_Security.Models
{
    [Table("Usuarios", Schema = "sec")]
    public class Usuario
    {
        [Key]
        public int usuario_id { get; set; }

        [Required]
        [StringLength(60)]
        public string username { get; set; }

        [Required]
        [StringLength(254)]
        public string email { get; set; }

        [Required]
        public byte[] password_hash { get; set; }

        [StringLength(1)]
        public string estado { get; set; } = "A";

        public DateTime creado_en { get; set; }
    }
}
