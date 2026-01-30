using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RAMAVE_Cotizador.Models
{
    public class Presupuesto
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string NombreCliente { get; set; } = string.Empty;

        // Estos son los campos nuevos que agregaste a la DB
        public string? Numero { get; set; }
        public string? Direccion { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public string? Observaciones { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPresupuesto { get; set; }

        public int? UsuarioId { get; set; }

        public virtual ICollection<Cotizaciones> Cotizaciones { get; set; } = new List<Cotizaciones>();
    }
}