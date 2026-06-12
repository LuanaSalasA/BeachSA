
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API_Security.Models
{
    [Table("RefreshToken", Schema = "sec")]
    public class RefreshToken
    {
        [Key]
        public Guid token_id { get; set; }

        public int usuario_id { get; set; }

        [Required]
        [StringLength(500)]
        public string token { get; set; }

        [Required]
        public DateTime expira_en { get; set; }

        public bool revocado { get; set; }

        public DateTime creado_en { get; set; }

       
    }
}