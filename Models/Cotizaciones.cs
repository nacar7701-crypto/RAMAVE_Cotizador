using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization; // Asegúrate de tener este using

namespace RAMAVE_Cotizador.Models
{
public class Cotizaciones {
    [Key] public int Id { get; set; }
    public int IdTela { get; set; }
    public string? Catalogo { get; set; }
    public string? Marca { get; set; }
    public string? Modelo { get; set; }
    public string? Cliente { get; set; }
    public decimal Ancho { get; set; }
    public decimal Alto { get; set; }
    public decimal M2 { get; set; }
    public int PorcentajeOnda { get; set; }
    public string? Acoplamiento { get; set; }
    public string? TipoApertura { get; set; }
    public string? Instalacion { get; set; } 
    public string? TipoCortina { get; set; }
    public string? Sistema { get; set; }
    public string? Area { get; set; }

    // Campos de ingeniería
    public decimal AnchoCM { get; set; }
    public decimal AlturaCM { get; set; }
    public int CantidadBroches { get; set; }
    public decimal CintaBroches { get; set; }
    public decimal TotalAnchoLienzo { get; set; }
    public decimal TotalAltura { get; set; }
    public decimal AlturaExacta { get; set; }
    public decimal CintaPlomo { get; set; }
    public int CarroMaestro { get; set; }
    public int CorrederasRipple { get; set; }
    public decimal AnchoRollo { get; set; }
    public int NumLienzos { get; set; }
    public decimal TotalML { get; set; }
    public int MLComprar { get; set; }
    public decimal Riel { get; set; }
    public int Tapon { get; set; }
    public int Baston { get; set; }
    public int Soportes { get; set; }
    public int UnionRiel { get; set; }
    public decimal Tarlatana { get; set; }
    public int CarroEmbaladoFrances { get; set; }
    public decimal Ganchos { get; set; }
    public int TopeCarritoSujecion { get; set; }
    public int TopeMecanismoCortinero { get; set; }
    public int Engrane { get; set; }
    public int TapasEngrane { get; set; }
    public int Hebilla { get; set; }
    public int CorrederaGoma { get; set; }
    public decimal GomaVerde { get; set; }
    public int PesaPlomo { get; set; }
    public decimal CarritoCortinero { get; set; }
    public int GanchoFinal { get; set; }


    // costos 
    public decimal CostoRiel { get; set; }
    public decimal CostoCarroMaestro { get; set; }
    public decimal CostoCorredera { get; set; }
    public decimal CostoCintaBroches { get; set; }
    public decimal CostoTapon { get; set; }
    public decimal CostoBaston { get; set; }
    public decimal CostoSoportes { get; set; }
    public decimal CostoUnionRiel { get; set; }
    public decimal CostoEmpaque { get; set; }
    public decimal CostoTarlatana { get; set; }
    public decimal CostoTotalCortinero { get; set; }
    public decimal PrecioTelaML { get; set; }
    public decimal CostoTotalTela { get; set; }
    public decimal CostoTotalCortina { get; set; }
    public decimal CostoTotalGeneral { get; set; }
    public decimal PrecioCortineroPublico { get; set; }
    public decimal PrecioCortinaPublico { get; set; }
    public decimal TotalPublico { get; set; }
    public decimal PrecioCortineroDistribuidor { get; set; }
    public decimal PrecioCortinaDistribuidor { get; set; }
    public decimal TotalDistribuidor { get; set; }
    public decimal CostoCarroEmbaladoFrances { get; set; }
    public decimal CostoGanchos { get; set; }
    public decimal Motor { get; set; }
    public decimal CostoTopeCarritoSujecion { get; set; }
    public decimal CostoTopeMecanismoCortinero { get; set; }
    public decimal CostoEngrane { get; set; }
    public decimal CostoTapasEngrane { get; set; }
    public decimal CostoHebilla { get; set; }
    public decimal CostoCorreaGoma { get; set; }
    public decimal CostoGomaVerde { get; set; }
    public decimal CostoPesaPlomo { get; set; }
    public decimal CostoCarritos { get; set; }
    public decimal CostoCarritoCortinero { get; set; }
    public decimal CostoGanchoFinal { get; set; }
}
}