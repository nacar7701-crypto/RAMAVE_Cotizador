using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace RAMAVE_Cotizador.Models;
public class Presupuesto
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string NombreCliente { get; set; } = string.Empty;

    public DateTime FechaCreacion { get; set; } = DateTime.Now;

    public string? Observaciones { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalPresupuesto { get; set; }

    public int? UsuarioId { get; set; }

    // USA SOLO ESTA. Borra la que dice "Partidas".
    public virtual ICollection<Cotizaciones> Cotizaciones { get; set; } = new List<Cotizaciones>();
}