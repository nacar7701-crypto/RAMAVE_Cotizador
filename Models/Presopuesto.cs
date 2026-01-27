using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace RAMAVE_Cotizador.Models;
public class Presupuesto
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(150)]
    public string NombreCliente { get; set; } // La persona a la que le cotizas

    public DateTime FechaCreacion { get; set; } = DateTime.Now;

    [StringLength(200)]
    public string? Observaciones { get; set; }

    // El TotalGeneral se puede calcular sumando las cotizaciones ligadas
    public decimal TotalPresupuesto { get; set; }

    // Relación: Un presupuesto tiene muchas cotizaciones (partidas)
    public virtual ICollection<Cotizaciones> Partidas { get; set; } = new List<Cotizaciones>();
}