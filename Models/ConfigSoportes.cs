using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RAMAVE_Cotizador.Models
{
    [Table("config_soportes")]
    public class ConfigSoportes
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("ancho_min")]
        public decimal AnchoMin { get; set; }

        [Column("ancho_max")]
        public decimal AnchoMax { get; set; }

        [Column("cantidad_soportes")]
        public int CantidadSoportes { get; set; }
    }
}