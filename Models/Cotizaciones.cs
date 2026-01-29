using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;


namespace RAMAVE_Cotizador.Models
{
    [Table("Cotizaciones")]
    public class Cotizaciones 
    {
        [Key] public int Id { get; set; }
        public int? IdTela { get; set; }
        public string? Catalogo { get; set; } = string.Empty;
        public string? Marca { get; set; } = string.Empty;
        public string? Modelo { get; set; } = string.Empty;
        public string? Cliente { get; set; } = string.Empty;
        public string? ColorSeleccionado { get; set; } = string.Empty;
        public string? tipo { get; set; } = string.Empty;
        
        // Campos de medida inicializados en 0
        public decimal Ancho { get; set; } = 0m;
        public decimal Alto { get; set; } = 0m;
        public decimal M2 { get; set; } = 0m;
        public int PorcentajeOnda { get; set; } = 0;
        
        public string? Acoplamiento { get; set; } = string.Empty;
        public string? TipoApertura { get; set; } = string.Empty;
        public string? Instalacion { get; set; } = string.Empty;
        public string? TipoCortina { get; set; } = string.Empty;
        public string? Sistema { get; set; } = string.Empty;
        public string? Area { get; set; } = string.Empty;

        // Campos de ingeniería inicializados en 0
        public decimal AnchoCM { get; set; } = 0m;
        public decimal AlturaCM { get; set; } = 0m;
        public int CantidadBroches { get; set; } = 0;
        public decimal CintaBroches { get; set; } = 0m;
        public decimal TotalAnchoLienzo { get; set; } = 0m;
        public decimal TotalAltura { get; set; } = 0m;
        public decimal AlturaExacta { get; set; } = 0m;
        public decimal CintaPlomo { get; set; } = 0m;
        public int CarroMaestro { get; set; } = 0;
        public int CorrederasRipple { get; set; } = 0;
        public decimal AnchoRollo { get; set; } = 0m;
        public int NumLienzos { get; set; } = 0;
        public decimal TotalML { get; set; } = 0m;
        public int MLComprar { get; set; } = 0;
        public decimal Riel { get; set; } = 0m;
        public int Tapon { get; set; } = 0;
        public int Baston { get; set; } = 0;
        public int Soportes { get; set; } = 0;
        public int UnionRiel { get; set; } = 0;
        public decimal Tarlatana { get; set; } = 0m;
        public int CarroEmbaladoFrances { get; set; } = 0;
        public decimal Ganchos { get; set; } = 0m;
        public int TopeCarritoSujecion { get; set; } = 0;
        public int TopeMecanismoCortinero { get; set; } = 0;
        public int Engrane { get; set; } = 0;
        public int TapasEngrane { get; set; } = 0;
        public int Hebilla { get; set; } = 0;
        public int CorrederaGoma { get; set; } = 0;
        public decimal GomaVerde { get; set; } = 0m;
        public int PesaPlomo { get; set; } = 0;
        public decimal CarritoCortinero { get; set; } = 0m;
        public int GanchoFinal { get; set; } = 0;

        // Costos inicializados en 0
        public decimal CostoRiel { get; set; } = 0m;
        public decimal CostoCarroMaestro { get; set; } = 0m;
        public decimal CostoCorredera { get; set; } = 0m;
        public decimal CostoCintaBroches { get; set; } = 0m;
        public decimal CostoTapon { get; set; } = 0m;
        public decimal CostoBaston { get; set; } = 0m;
        public decimal CostoSoportes { get; set; } = 0m;
        public decimal CostoUnionRiel { get; set; } = 0m;
        public decimal CostoEmpaque { get; set; } = 0m;
        public decimal CostoTarlatana { get; set; } = 0m;
        public decimal CostoTotalCortinero { get; set; } = 0m;
        public decimal PrecioTelaML { get; set; } = 0m;
        public decimal CostoTotalTela { get; set; } = 0m;
        public decimal CostoTotalCortina { get; set; } = 0m;
        public decimal CostoTotalGeneral { get; set; } = 0m;
        public decimal PrecioCortineroPublico { get; set; } = 0m;
        public decimal PrecioCortinaPublico { get; set; } = 0m;
        public decimal TotalPublico { get; set; } = 0m;
        public decimal PrecioCortineroDistribuidor { get; set; } = 0m;
        public decimal PrecioCortinaDistribuidor { get; set; } = 0m;
        public decimal TotalDistribuidor { get; set; } = 0m;
        public decimal CostoCarroEmbaladoFrances { get; set; } = 0m;
        public decimal CostoGanchos { get; set; } = 0m;
        public decimal Motor { get; set; } = 0m;
        public decimal CostoTopeCarritoSujecion { get; set; } = 0m;
        public decimal CostoTopeMecanismoCortinero { get; set; } = 0m;
        public decimal CostoEngrane { get; set; } = 0m;
        public decimal CostoTapasEngrane { get; set; } = 0m;
        public decimal CostoHebilla { get; set; } = 0m;
        public decimal CostoCorreaGoma { get; set; } = 0m;
        public decimal CostoGomaVerde { get; set; } = 0m;
        public decimal CostoPesaPlomo { get; set; } = 0m;
        public decimal CostoCarritos { get; set; } = 0m;
        public decimal CostoCarritoCortinero { get; set; } = 0m;
        public decimal CostoGanchoFinal { get; set; } = 0m;
        [NotMapped] // Esto le dice a EF: "No busques esta columna en la tabla Cotizaciones"
        public string? NombreCliente { get; set; }

        public int? PresupuestoId { get; set; } 

        [ForeignKey("PresupuestoId")]
        [JsonIgnore]
        public virtual Presupuesto? Presupuesto { get; set; }
    }
}