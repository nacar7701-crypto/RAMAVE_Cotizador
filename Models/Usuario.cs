using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RAMAVE_Cotizador.Models
{
    [Table("usuarios")] // Nombre exacto de la tabla en tu Docker
    public class Usuario
    {
        [Key]
        public int id { get; set; }
        public string nombre { get; set; } = null!;
        public string correo { get; set; } = null!;
        public string password { get; set; } = null!;
        public string rol { get; set; } = "vendedor";
        public DateTime fecha_creacion { get; set; } = DateTime.Now;
    }
}

public class LoginRequest
{
    public string Correo { get; set; } = null!;
    public string Password { get; set; } = null!;
}