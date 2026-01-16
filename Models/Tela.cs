using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RAMAVE_Cotizador.Models
{
    [Table("marcas")]
    public class Marca
    {
        [Key]
        public int id { get; set; }

        [Required]
        public string? nombre { get; set; }

        // AÑADE ESTA LÍNEA para quitar el error CS1061
        public virtual ICollection<Modelo> modelos { get; set; } = new List<Modelo>();
    }
    
    [Table("modelos")]
    public class Modelo
    {
        [Key]
        public int id { get; set; }

        [Required]
        public string? nombre { get; set; }

        public int id_marca { get; set; }

        [ForeignKey("id_marca")]
        public virtual Marca? marca { get; set; }
    }

    [Table("telas")] 
    public class Tela
    {
        [Key]
        public int id { get; set; }

        [Required]
        public int id_modelo { get; set; }

        [ForeignKey("id_modelo")]
        public virtual Modelo? modelo { get; set; }

        public string? catalogo { get; set; }
        public string? color_nombre { get; set; }
        [NotMapped]
        public List<string> ListaColores
        {
            get => string.IsNullOrEmpty(color_nombre) 
                ? new List<string>() 
                : color_nombre.Split(',').Select(c => c.Trim()).ToList();
            set => color_nombre = value != null ? string.Join(",", value) : null;
        }
        [NotMapped]
        public List<string>? ColoresNuevos { get; set; }
        public string? tipo { get; set; } 

        [Column(TypeName = "decimal(18,2)")]
        public decimal existencia { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal ancho { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal precio_ml_corte { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal precio_ml_rollo { get; set; }

        public string? lavar { get; set; } 
        public string? temp_agua { get; set; }
        public string? exprimir { get; set; }
        public string? planchar { get; set; }
        public string? blanqueador { get; set; }
        public string? jabon { get; set; }

        // --- PROPIEDADES CALCULADAS ---
        [NotMapped]
        public decimal precio_ml_corte_iva => Math.Round(precio_ml_corte * 1.16m, 4);

        [NotMapped]
        public decimal precio_ml_rollo_iva => Math.Round(precio_ml_rollo * 1.16m, 4);

        [NotMapped]
        public decimal costo_x_m2 => ancho > 0 ? Math.Round(precio_ml_rollo_iva / ancho, 4) : 0;
    }
}