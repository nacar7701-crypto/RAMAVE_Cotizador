using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RAMAVE_Cotizador.Models
{
    [Table("usuarios")]
    public class Usuario
    {
        [Key]
        public int id { get; set; }

        public string nombre { get; set; } = null!;

        public string correo_electronico { get; set; } = null!; // Antes era "correo"

        public string password { get; set; } = null!; // "Contraseña"

        public string rol { get; set; } = "Tienda";

        public DateTime fecha_creacion { get; set; } = DateTime.Now;
    }

    public class LoginRequest
    {
        public string Correo { get; set; } = null!;
        public string Password { get; set; } = null!;
    }
}