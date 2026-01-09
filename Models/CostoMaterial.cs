using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RAMAVE_Cotizador.Models
{
    [Table("costos_materiales")]
    public class CostoMaterial
    {
        [Key]
        public int id { get; set; }

        public string sistema { get; set; } = null!;
        public string tipo { get; set; } = null!;
        public string concepto { get; set; } = null!;
        public string um { get; set; } = "1"; // Unidad de Medida

        public decimal precio_unitario { get; set; }

        // Propiedad calculada que no está en la base de datos
        [NotMapped]
        public decimal precio_con_iva 
        { 
            get { return precio_unitario * 1.16m; } 
        }
    }
}