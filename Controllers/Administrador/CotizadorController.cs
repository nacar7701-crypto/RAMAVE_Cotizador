using Microsoft.AspNetCore.Mvc;
using RAMAVE_Cotizador.Data;
using RAMAVE_Cotizador.Models;
using Microsoft.EntityFrameworkCore;

namespace RAMAVE_Cotizador.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CotizadorController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CotizadorController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("Crear")]
        public async Task<IActionResult> PostCotizacion([FromBody] Cotizaciones model)
        {
            var tela = await _context.Telas
                .Include(t => t.modelo!)
                .ThenInclude(m => m.marca!)
                .FirstOrDefaultAsync(t => t.id == model.IdTela);

            if (tela == null) return BadRequest("Tela no encontrada");

            string tipoCortina = model.TipoCortina?.ToUpper() ?? "";
            string sistema = model.Sistema?.ToUpper() ?? "";

            // --- SECCIÓN 1: CAMPOS COMUNES ---
            model.Catalogo = tela.catalogo;
            model.Marca = tela.modelo?.marca?.nombre ?? "N/A";
            model.Modelo = tela.modelo?.nombre ?? "N/A";
            model.AnchoRollo = tela.ancho;
            model.ColorSeleccionado = model.ColorSeleccionado;
            model.AnchoCM = model.Ancho * 100;
            model.AlturaCM = model.Alto * 100;
            model.M2 = model.Ancho * model.Alto;
            model.TipoApertura = (model.Acoplamiento?.ToUpper() == "AMBOS") ? "DOS HOJAS" : "UNA HOJA";

            // ============================================================
            // BLOQUE 1: ONDULADO Y MANUAL
            // ============================================================
            if (tipoCortina == "ONDULADO" && sistema == "MANUAL")
            {
                decimal factor = model.PorcentajeOnda == 60 ? (10.8m / 6.8m) :
                                 model.PorcentajeOnda == 80 ? (10.8m / 6.1m) : (10.8m / 4.5m);

                decimal brochesRaw = (model.AnchoCM * factor) / 10.8m;
                model.CantidadBroches = (int)Math.Ceiling(brochesRaw);
                if (model.CantidadBroches % 2 != 0) model.CantidadBroches++;

                model.CintaBroches = Math.Round(((model.CantidadBroches * 10.8m) + 5) / 100, 2);
                model.TotalAnchoLienzo = model.CintaBroches + 0.52m; // 0.16+0.16+0.1+0.1
                model.TotalAltura = (model.AlturaCM + 44) / 100;
                model.AlturaExacta = model.TotalAltura;
                model.NumLienzos = (int)Math.Ceiling(model.TotalAnchoLienzo / model.AnchoRollo);

                model.CarroMaestro = (model.TipoApertura == "DOS HOJAS") ? 2 : 1;
                model.Tapon = 2;
                model.Baston = model.CarroMaestro;
                model.CorrederasRipple = Math.Abs((model.CarroMaestro * 2) - model.CantidadBroches);

                model.TotalML = model.AlturaExacta > model.AnchoRollo ? (model.NumLienzos * model.AlturaExacta) : model.TotalAnchoLienzo;
                model.MLComprar = (int)Math.Ceiling(model.TotalML);
                model.Riel = model.Ancho;
                model.Tarlatana = model.CintaBroches;
                model.CintaPlomo = model.CintaBroches;
                model.UnionRiel = model.Ancho > 5.8m ? 1 : 0;

                string instSafe = (model.Instalacion ?? "MURO").ToUpper();
                if (instSafe.Contains("TECHO")) model.Soportes = 0;
                else
                {
                    var sop = await _context.ConfigSoportes.FirstOrDefaultAsync(s => model.Ancho >= s.AnchoMin && model.Ancho <= s.AnchoMax);
                    model.Soportes = sop?.CantidadSoportes ?? 0;
                }

                async Task<decimal> PrecioMat(string c)
                {
                    var m = await _context.CostosMateriales.FirstOrDefaultAsync(x => x.concepto.ToUpper() == c);
                    return (m?.precio_unitario ?? 0m) * 1.16m;
                }

                model.CostoRiel = (await PrecioMat("RIEL TECHO O MURO")) * model.Riel;
                model.CostoCarroMaestro = (await PrecioMat("CARRO MAESTRO IZQ - DER BROCHE DE METAL")) * (decimal)model.CarroMaestro;
                model.CostoCorredera = (await PrecioMat("CORREDERA DE 2 LLANTAS RIPPLEFOLD")) * (decimal)model.CorrederasRipple;
                model.CostoCintaBroches = (await PrecioMat("CINTA CON BROCHES")) * model.CintaBroches;
                model.CostoTapon = (await PrecioMat("TAPON RIPPLEFOLD")) * (decimal)model.Tapon;
                model.CostoBaston = (await PrecioMat("BASTON")) * (decimal)model.Baston;
                model.CostoSoportes = (await PrecioMat(instSafe.Contains("TECHO") ? "SOPORTE A TECHO" : "SOPORTE A MURO")) * (decimal)model.Soportes;
                model.CostoUnionRiel = model.UnionRiel > 0 ? (await PrecioMat("UNION PARA RIEL")) : 0m;
                model.CostoTarlatana = (await PrecioMat("TARLATANA")) * model.Tarlatana;
                model.CostoEmpaque = 20.00m;

                model.CostoTotalCortinero = model.CostoRiel + model.CostoCarroMaestro + model.CostoCorredera + model.CostoCintaBroches + model.CostoTapon + model.CostoBaston + model.CostoSoportes + model.CostoUnionRiel + model.CostoEmpaque;
                model.PrecioTelaML = tela.precio_ml_corte * 1.16m;
                model.CostoTotalTela = (decimal)model.MLComprar * model.PrecioTelaML;
                model.CostoTotalCortina = model.CostoTotalTela + model.CostoCintaBroches + model.CostoEmpaque + model.CostoTarlatana;
                model.CostoTotalGeneral = model.CostoTotalCortinero + model.CostoTotalCortina;

                // --- COMERCIAL (DENTRO DEL IF DE ONDULADO) ---
                model.PrecioTelaML = tela.precio_ml_corte * 1.16m;
                model.CostoTotalTela = (decimal)model.MLComprar * model.PrecioTelaML;
                model.CostoTotalCortina = model.CostoTotalTela + model.CostoCintaBroches + model.CostoEmpaque + model.CostoTarlatana;
                model.CostoTotalGeneral = model.CostoTotalCortinero + model.CostoTotalCortina;

                // PÚBLICO (x3) - AQUÍ ESTABA EL FALTANTE
                model.PrecioCortineroPublico = model.CostoTotalCortinero * 3;
                model.PrecioCortinaPublico = model.CostoTotalCortina * 3;
                model.TotalPublico = model.CostoTotalGeneral * 3;

                // DISTRIBUIDOR (x2) - AQUÍ ESTABA EL FALTANTE
                model.PrecioCortineroDistribuidor = model.CostoTotalCortinero * 2;
                model.PrecioCortinaDistribuidor = model.CostoTotalCortina * 2;
                model.TotalDistribuidor = model.CostoTotalGeneral * 2;

                model.TotalPublico = model.CostoTotalGeneral * 3;
                model.TotalDistribuidor = model.CostoTotalGeneral * 2;
            }

            // ============================================================
            // BLOQUE 2: ONDULADO Y MOTORIZADO WIFI
            // ============================================================
            else if (tipoCortina == "ONDULADO" && sistema == "MOTORIZADO WIFI")
            {
                // --- 1. INGENIERÍA ---
                decimal factor = model.PorcentajeOnda == 60 ? (10.8m / 6.8m) :
                                model.PorcentajeOnda == 80 ? (10.8m / 6.1m) : (10.8m / 4.5m);

                decimal brochesRaw = (model.AnchoCM * factor) / 10.8m;
                model.CantidadBroches = (int)Math.Ceiling(brochesRaw);
                if (model.CantidadBroches % 2 != 0) model.CantidadBroches++;

                model.CintaBroches = Math.Round(((model.CantidadBroches * 10.8m) + 5) / 100, 2);
                model.TotalAnchoLienzo = model.CintaBroches + 0.52m;
                model.TotalAltura = (model.AlturaCM + 44) / 100;
                model.AlturaExacta = model.TotalAltura;
                model.NumLienzos = (int)Math.Ceiling(model.TotalAnchoLienzo / model.AnchoRollo);
                model.TotalML = model.AlturaExacta > model.AnchoRollo ? (model.NumLienzos * model.AlturaExacta) : model.TotalAnchoLienzo;
                model.MLComprar = (int)Math.Ceiling(model.TotalML);

                // Piezas Fijas
                model.TopeCarritoSujecion = 1;
                model.TopeMecanismoCortinero = 1;
                model.Engrane = 2;
                model.TapasEngrane = 2;
                model.PesaPlomo = 4;
                model.Riel = model.Ancho;
                model.GomaVerde = (model.Ancho * 2) + 0.3m;
                model.UnionRiel = model.Ancho > 5.8m ? 1 : 0;

                if (model.TipoApertura == "DOS HOJAS")
                {
                    model.Hebilla = 2; model.CorrederaGoma = 2; model.CarroMaestro = 2;
                }
                else
                {
                    model.Hebilla = 1; model.CorrederaGoma = 1; model.CarroMaestro = 1;
                }

                // Soportes
                string inst = (model.Instalacion ?? "MURO").ToUpper();
                var sopConfig = await _context.ConfigSoportes.FirstOrDefaultAsync(s => model.Ancho >= s.AnchoMin && model.Ancho <= s.AnchoMax);
                model.Soportes = inst.Contains("TECHO") ? 0 : (sopConfig?.CantidadSoportes ?? 0);

                // --- 2. COSTOS (SISTEMA MOTORIZADO) ---
                async Task<decimal> GetP(string concepto)
                {
                    var m = await _context.CostosMateriales.FirstOrDefaultAsync(x => x.concepto.ToUpper() == concepto.ToUpper() && x.sistema.ToUpper() == "MOTORIZADO");
                    return (m?.precio_unitario ?? 0m) * 1.16m;
                }

                model.Motor = await GetP("MOTOR WIFI + CONTROL");
                model.CostoRiel = (await GetP("RIEL PARA CORTINERO MOTORIZADO")) * model.Riel;

                // Carro Maestro dinámico
                string conceptoCarro = (model.CarroMaestro == 2) ? "CARRO MAESTRO 2HOJA RIPP MOTOR" : "CARRO MAESTRO 1HOJA RIPP MOTOR";
                model.CostoCarroMaestro = (await GetP(conceptoCarro)) * (decimal)model.CarroMaestro;

                model.CostoTopeCarritoSujecion = (await GetP("TOPE DE CARRITOS SUJECION")) * (decimal)model.TopeCarritoSujecion;
                model.CostoTopeMecanismoCortinero = (await GetP("TOPE MECANISMO DE CORTINERO")) * (decimal)model.TopeMecanismoCortinero;
                model.CostoCarritos = (await GetP("CARRITO PARA CORTINERO ONDULADO")) * (decimal)model.CantidadBroches;
                model.CostoSoportes = (await GetP(inst.Contains("TECHO") ? "SOPORTE A TECHO" : "SOPORTE PARED")) * (decimal)model.Soportes;
                model.CostoEngrane = (await GetP("ENGRANE CORTINERO W")) * (decimal)model.Engrane;
                model.CostoUnionRiel = (await GetP("CONECTOR PERF - UNION")) * (decimal)model.UnionRiel;
                model.CostoTapasEngrane = (await GetP("TAPA PARA SISTEMA DE ENGRANE")) * (decimal)model.TapasEngrane;
                model.CostoHebilla = (await GetP("HEBILLA PARA CINTA CORTINERO RIPP")) * (decimal)model.Hebilla;
                model.CostoCorreaGoma = (await GetP("CORREA DE GOMA")) * (decimal)model.CorrederaGoma;
                model.CostoCintaBroches = (await GetP("CINTA CON BROCHES")) * model.CintaBroches;
                model.CostoGomaVerde = (await GetP("GOMA VERDE")) * model.GomaVerde;
                model.CostoPesaPlomo = (await GetP("PESO PLOMO")) * (decimal)model.PesaPlomo;
                model.CostoEmpaque = 20.00m;

                // SUMA TOTAL CORTINERO
                model.CostoTotalCortinero = model.Motor + model.CostoRiel + model.CostoCarroMaestro + model.CostoTopeCarritoSujecion +
                                            model.CostoTopeMecanismoCortinero + model.CostoCarritos + model.CostoSoportes +
                                            model.CostoEngrane + model.CostoUnionRiel + model.CostoTapasEngrane +
                                            model.CostoHebilla + model.CostoCorreaGoma + model.CostoEmpaque +
                                            model.CostoCintaBroches + model.CostoPesaPlomo + model.CostoGomaVerde;

                // --- 3. COMERCIAL ---
                model.PrecioTelaML = tela.precio_ml_corte * 1.16m;
                model.CostoTotalTela = (decimal)model.MLComprar * model.PrecioTelaML;

                // COSTO TOTAL CORTINA: Tela + Pesa Plomo + Cinta Broches + Empaque
                model.CostoTotalCortina = model.CostoTotalTela + model.CostoPesaPlomo + model.CostoCintaBroches + model.CostoEmpaque;
                model.CostoTotalGeneral = model.CostoTotalCortinero + model.CostoTotalCortina;

                // Totales x3 y x2
                model.PrecioCortineroPublico = model.CostoTotalCortinero * 3;
                model.PrecioCortinaPublico = model.CostoTotalCortina * 3;
                model.TotalPublico = model.CostoTotalGeneral * 3;
                model.PrecioCortineroDistribuidor = model.CostoTotalCortinero * 2;
                model.PrecioCortinaDistribuidor = model.CostoTotalCortina * 2;
                model.TotalDistribuidor = model.CostoTotalGeneral * 2;
            }

            // ============================================================
            // BLOQUE 3: ONDULADO Y MOTORIZADO BATERIA
            // ============================================================
            else if (tipoCortina == "ONDULADO" && sistema == "MOTORIZADO BATERIA")
            {
                // --- 1. INGENIERÍA ---
                decimal factor = model.PorcentajeOnda == 60 ? (10.8m / 6.8m) :
                                model.PorcentajeOnda == 80 ? (10.8m / 6.1m) : (10.8m / 4.5m);

                decimal brochesRaw = (model.AnchoCM * factor) / 10.8m;
                model.CantidadBroches = (int)Math.Ceiling(brochesRaw);
                if (model.CantidadBroches % 2 != 0) model.CantidadBroches++;

                model.CintaBroches = Math.Round(((model.CantidadBroches * 10.8m) + 5) / 100, 2);
                model.TotalAnchoLienzo = model.CintaBroches + 0.52m;
                model.TotalAltura = (model.AlturaCM + 44) / 100;
                model.AlturaExacta = model.TotalAltura;
                model.NumLienzos = (int)Math.Ceiling(model.TotalAnchoLienzo / model.AnchoRollo);
                model.TotalML = model.AlturaExacta > model.AnchoRollo ? (model.NumLienzos * model.AlturaExacta) : model.TotalAnchoLienzo;
                model.MLComprar = (int)Math.Ceiling(model.TotalML);

                // Piezas Fijas
                model.TopeCarritoSujecion = 1;
                model.TopeMecanismoCortinero = 1;
                model.Engrane = 2;
                model.TapasEngrane = 2;
                model.PesaPlomo = 4;
                model.Riel = model.Ancho;
                model.GomaVerde = (model.Ancho * 2) + 0.3m;
                model.UnionRiel = model.Ancho > 5.8m ? 1 : 0;

                if (model.TipoApertura == "DOS HOJAS")
                {
                    model.Hebilla = 2; model.CorrederaGoma = 2; model.CarroMaestro = 2;
                }
                else
                {
                    model.Hebilla = 1; model.CorrederaGoma = 1; model.CarroMaestro = 1;
                }

                // Soportes
                string inst = (model.Instalacion ?? "MURO").ToUpper();
                var sopConfig = await _context.ConfigSoportes.FirstOrDefaultAsync(s => model.Ancho >= s.AnchoMin && model.Ancho <= s.AnchoMax);
                model.Soportes = inst.Contains("TECHO") ? 0 : (sopConfig?.CantidadSoportes ?? 0);

                // --- 2. COSTOS (SISTEMA MOTORIZADO) ---
                async Task<decimal> GetP(string concepto)
                {
                    var m = await _context.CostosMateriales.FirstOrDefaultAsync(x => x.concepto.ToUpper() == concepto.ToUpper() && x.sistema.ToUpper() == "MOTORIZADO");
                    return (m?.precio_unitario ?? 0m) * 1.16m;
                }

                model.Motor = await GetP("MOTOR BATERIAS + CONTROL + CARGADOR");
                model.CostoRiel = (await GetP("RIEL PARA CORTINERO MOTORIZADO")) * model.Riel;

                // Carro Maestro dinámico
                string conceptoCarro = (model.CarroMaestro == 2) ? "CARRO MAESTRO 2HOJA RIPP MOTOR" : "CARRO MAESTRO 1HOJA RIPP MOTOR";
                model.CostoCarroMaestro = (await GetP(conceptoCarro)) * (decimal)model.CarroMaestro;

                model.CostoTopeCarritoSujecion = (await GetP("TOPE DE CARRITOS SUJECION")) * (decimal)model.TopeCarritoSujecion;
                model.CostoTopeMecanismoCortinero = (await GetP("TOPE MECANISMO DE CORTINERO")) * (decimal)model.TopeMecanismoCortinero;
                model.CostoCarritos = (await GetP("CARRITO PARA CORTINERO ONDULADO")) * (decimal)model.CantidadBroches;
                model.CostoSoportes = (await GetP(inst.Contains("TECHO") ? "SOPORTE A TECHO" : "SOPORTE PARED")) * (decimal)model.Soportes;
                model.CostoEngrane = (await GetP("ENGRANE CORTINERO W")) * (decimal)model.Engrane;
                model.CostoUnionRiel = (await GetP("CONECTOR PERF - UNION")) * (decimal)model.UnionRiel;
                model.CostoTapasEngrane = (await GetP("TAPA PARA SISTEMA DE ENGRANE")) * (decimal)model.TapasEngrane;
                model.CostoHebilla = (await GetP("HEBILLA PARA CINTA CORTINERO RIPP")) * (decimal)model.Hebilla;
                model.CostoCorreaGoma = (await GetP("CORREA DE GOMA")) * (decimal)model.CorrederaGoma;
                model.CostoCintaBroches = (await GetP("CINTA CON BROCHES")) * model.CintaBroches;
                model.CostoGomaVerde = (await GetP("GOMA VERDE")) * model.GomaVerde;
                model.CostoPesaPlomo = (await GetP("PESO PLOMO")) * (decimal)model.PesaPlomo;
                model.CostoEmpaque = 20.00m;

                // SUMA TOTAL CORTINERO
                model.CostoTotalCortinero = model.Motor + model.CostoRiel + model.CostoCarroMaestro + model.CostoTopeCarritoSujecion +
                                            model.CostoTopeMecanismoCortinero + model.CostoCarritos + model.CostoSoportes +
                                            model.CostoEngrane + model.CostoUnionRiel + model.CostoTapasEngrane +
                                            model.CostoHebilla + model.CostoCorreaGoma + model.CostoEmpaque +
                                            model.CostoCintaBroches + model.CostoPesaPlomo + model.CostoGomaVerde;

                // --- 3. COMERCIAL ---
                model.PrecioTelaML = tela.precio_ml_corte * 1.16m;
                model.CostoTotalTela = (decimal)model.MLComprar * model.PrecioTelaML;

                // COSTO TOTAL CORTINA: Tela + Pesa Plomo + Cinta Broches + Empaque
                model.CostoTotalCortina = model.CostoTotalTela + model.CostoPesaPlomo + model.CostoCintaBroches + model.CostoEmpaque;
                model.CostoTotalGeneral = model.CostoTotalCortinero + model.CostoTotalCortina;

                // Totales x3 y x2
                model.PrecioCortineroPublico = model.CostoTotalCortinero * 3;
                model.PrecioCortinaPublico = model.CostoTotalCortina * 3;
                model.TotalPublico = model.CostoTotalGeneral * 3;
                model.PrecioCortineroDistribuidor = model.CostoTotalCortinero * 2;
                model.PrecioCortinaDistribuidor = model.CostoTotalCortina * 2;
                model.TotalDistribuidor = model.CostoTotalGeneral * 2;
            }
            // ============================================================
            // BLOQUE 4: FRANCESA Y MANUAL
            // ============================================================
            else if (tipoCortina == "FRANCESA" && sistema == "MANUAL")
            {
                model.CarroEmbaladoFrances = (int)Math.Floor(model.AnchoCM / 10) - 1;
                model.Tarlatana = ((model.CarroEmbaladoFrances * 10) + model.AnchoCM + 10) / 100;
                model.TotalAnchoLienzo = model.Tarlatana + 0.52m;
                model.TotalAltura = (model.AlturaCM + 44) / 100;
                model.AlturaExacta = model.TotalAltura;
                model.NumLienzos = (int)Math.Ceiling(model.TotalAnchoLienzo / model.AnchoRollo);

                model.TotalML = model.AlturaExacta > model.AnchoRollo ? (model.NumLienzos * model.AlturaExacta) : model.TotalAnchoLienzo;
                model.MLComprar = (int)Math.Ceiling(model.TotalML);
                model.Riel = model.Ancho;
                model.CarroMaestro = (model.TipoApertura == "DOS HOJAS") ? 2 : 1;
                model.Tapon = 2;
                model.Ganchos = model.CarroEmbaladoFrances + 2;
                model.Baston = model.CarroMaestro;
                model.UnionRiel = model.Ancho > 5.8m ? 1 : 0;

                string inst = (model.Instalacion ?? "MURO").ToUpper();
                if (inst.Contains("TECHO")) model.Soportes = 0;
                else
                {
                    var sop = await _context.ConfigSoportes.FirstOrDefaultAsync(s => model.Ancho >= s.AnchoMin && model.Ancho <= s.AnchoMax);
                    model.Soportes = sop?.CantidadSoportes ?? 0;
                }

                async Task<decimal> PrecioMatIva(string c)
                {
                    var m = await _context.CostosMateriales.FirstOrDefaultAsync(x => x.concepto.ToUpper() == c);
                    return (m?.precio_unitario ?? 0m) * 1.16m;
                }

                model.CostoRiel = (await PrecioMatIva("RIEL TECHO O MURO")) * model.Riel;
                model.CostoCarroMaestro = (await PrecioMatIva("CARRO MAESTRO IZQ - DER FRANCES")) * (decimal)model.CarroMaestro;
                model.CostoCarroEmbaladoFrances = (await PrecioMatIva("CARRO EMBALADO FRANCES")) * (decimal)model.CarroEmbaladoFrances;
                model.CostoTapon = (await PrecioMatIva("TAPON FRANCES")) * (decimal)model.Tapon;
                model.CostoGanchos = (await PrecioMatIva("GANCHOS METALICOS")) * (decimal)model.Ganchos;
                model.CostoBaston = (await PrecioMatIva("BASTON")) * (decimal)model.Baston;
                model.CostoSoportes = (await PrecioMatIva(inst.Contains("TECHO") ? "SOPORTE A TECHO" : "SOPORTE A MURO")) * (decimal)model.Soportes;
                model.CostoUnionRiel = model.UnionRiel > 0 ? (await PrecioMatIva("UNION PARA RIEL")) : 0m;
                model.CostoTarlatana = (await PrecioMatIva("TARLATANA")) * model.Tarlatana;
                model.CostoEmpaque = 20.00m;

                model.CostoTotalCortinero = model.CostoRiel + model.CostoCarroMaestro + model.CostoCarroEmbaladoFrances + model.CostoTapon + model.CostoGanchos + model.CostoBaston + model.CostoSoportes + model.CostoUnionRiel + model.CostoEmpaque;

                model.PrecioTelaML = tela.precio_ml_corte * 1.16m;

                model.CostoTotalTela = (decimal)model.MLComprar * model.PrecioTelaML;
                model.CostoTotalCortina = model.CostoTotalTela + model.CostoTarlatana + model.CostoEmpaque;
                model.CostoTotalGeneral = model.CostoTotalCortinero + model.CostoTotalCortina;

                model.PrecioCortineroPublico = model.CostoTotalCortinero * 3;
                model.PrecioCortinaPublico = model.CostoTotalCortina * 3;
                model.TotalPublico = model.CostoTotalGeneral * 3;
                model.PrecioCortineroDistribuidor = model.CostoTotalCortinero * 2;
                model.PrecioCortinaDistribuidor = model.CostoTotalCortina * 2;
                model.TotalDistribuidor = model.CostoTotalGeneral * 2;
            }
            // ============================================================
            // BLOQUE 5: FRANCESA Y MOTORIZADO ESTANDAR
            // ============================================================
            else if (tipoCortina == "FRANCESA" && sistema == "MOTORIZADO ESTANDAR")
            {
                // --- 1. INGENIERÍA (Igual que la manual) ---
                model.CarroEmbaladoFrances = (int)Math.Floor(model.AnchoCM / 10) - 1;
                model.Tarlatana = ((model.CarroEmbaladoFrances * 10) + model.AnchoCM + 10) / 100;
                model.TotalAnchoLienzo = model.Tarlatana + 0.52m;
                model.TotalAltura = (model.AlturaCM + 44) / 100;
                model.AlturaExacta = model.TotalAltura;
                model.NumLienzos = (int)Math.Ceiling(model.TotalAnchoLienzo / model.AnchoRollo);

                model.TotalML = model.AlturaExacta > model.AnchoRollo
                                ? (model.NumLienzos * model.AlturaExacta)
                                : model.TotalAnchoLienzo;

                model.MLComprar = (int)Math.Ceiling(model.TotalML);

                // --- 2. LÓGICA DE ACCESORIOS (Todo a 0 por ser motorizado) ---
                model.Riel = 0;
                model.CarroMaestro = 0;
                model.Tapon = 0;
                model.Ganchos = 0;
                model.Baston = 0;
                model.Soportes = 0;
                model.UnionRiel = 0;
                model.CarroEmbaladoFrances = 0;

                // --- 3. COSTOS (Valores en 0 y Costo Fijo) ---
                model.CostoRiel = 0;
                model.CostoCarroMaestro = 0;
                model.CostoCarroEmbaladoFrances = 0;
                model.CostoTapon = 0;
                model.CostoGanchos = 0;
                model.CostoBaston = 0;
                model.CostoSoportes = 0;
                model.CostoUnionRiel = 0;
                model.CostoTarlatana = 0; // Se suma en la cortina, no en el cortinero
                model.CostoEmpaque = 20.00m;

                // COSTO TOTAL CORTINERO FIJO
                model.CostoTotalCortinero = 2335.00m;
                model.Motor = 2335.00m;

                // --- 4. CÁLCULOS COMERCIALES (TELA Y TOTALES) ---
                model.PrecioTelaML = tela.precio_ml_corte * 1.16m;
                model.CostoTotalTela = (decimal)model.MLComprar * model.PrecioTelaML;

                // Aquí calculamos la tarlatana para la cortina (necesitamos el precio con IVA)
                var matTarlatana = await _context.CostosMateriales.FirstOrDefaultAsync(x => x.concepto.ToUpper() == "TARLATANA");
                decimal precioTarlatanaIva = (matTarlatana?.precio_unitario ?? 0m) * 1.16m;
                model.CostoTarlatana = precioTarlatanaIva * model.Tarlatana;

                // Costo Total Cortina
                model.CostoTotalCortina = model.CostoTotalTela + model.CostoTarlatana + model.CostoEmpaque;
                model.CostoTotalGeneral = model.CostoTotalCortinero + model.CostoTotalCortina;

                // PRECIOS PÚBLICO (x3)
                model.PrecioCortineroPublico = model.CostoTotalCortinero * 3;
                model.PrecioCortinaPublico = model.CostoTotalCortina * 3;
                model.TotalPublico = model.CostoTotalGeneral * 3;

                // PRECIOS DISTRIBUIDOR (x2)
                model.PrecioCortineroDistribuidor = model.CostoTotalCortinero * 2;
                model.PrecioCortinaDistribuidor = model.CostoTotalCortina * 2;
                model.TotalDistribuidor = model.CostoTotalGeneral * 2;
            }
            // ============================================================
            // BLOQUE 6: FRANCESA Y MOTORIZADO WIFI
            // ============================================================
            else if (tipoCortina == "FRANCESA" && sistema == "MOTORIZADO WIFI")
            {
                // --- 1. INGENIERÍA FRANCESA ---
                // Carrito de cortinero se calcula igual que el carro embalado francés manual
                model.CarritoCortinero = (model.AnchoCM / 10m) - 1m;
                model.GanchoFinal = 2; // Siempre 2
                model.Ganchos = model.CarritoCortinero + 2m;


                // Tarlatana: ((CarritoCortinero * 10) + AnchoCM) / 100
                model.Tarlatana = ((model.CarritoCortinero * 10) + model.AnchoCM) / 100;

                model.TotalAnchoLienzo = model.Tarlatana + 0.52m;
                model.TotalAltura = (model.AlturaCM + 44) / 100;
                model.AlturaExacta = model.TotalAltura;
                model.NumLienzos = (int)Math.Ceiling(model.TotalAnchoLienzo / model.AnchoRollo);

                model.TotalML = model.AlturaExacta > model.AnchoRollo
                                ? (model.NumLienzos * model.AlturaExacta)
                                : model.TotalAnchoLienzo;
                model.MLComprar = (int)Math.Ceiling(model.TotalML);

                // --- 2. MECÁNICA MOTORIZADA (Igual que ondulada wifi) ---
                model.TopeCarritoSujecion = 1;
                model.TopeMecanismoCortinero = 1;
                model.Engrane = 2;
                model.TapasEngrane = 0;
                model.PesaPlomo = 4;
                model.Riel = model.Ancho;
                model.GomaVerde = model.Riel * 2.05m;
                model.UnionRiel = model.Ancho > 5.8m ? 1 : 0;
                model.CorrederaGoma = 2;

                string inst = (model.Instalacion ?? "MURO").ToUpper();
                var sopConfig = await _context.ConfigSoportes.FirstOrDefaultAsync(s => model.Ancho >= s.AnchoMin && model.Ancho <= s.AnchoMax);
                model.Soportes = inst.Contains("TECHO") ? 0 : (sopConfig?.CantidadSoportes ?? 0);

                // --- 3. COSTOS (SISTEMA MOTORIZADO) ---
                async Task<decimal> GetPMot(string concepto)
                {
                    var m = await _context.CostosMateriales.FirstOrDefaultAsync(x => x.concepto.ToUpper() == concepto.ToUpper() && x.sistema.ToUpper() == "MOTORIZADO");
                    return (m?.precio_unitario ?? 0m) * 1.16m;
                }

                model.Motor = await GetPMot("MOTOR WIFI + CONTROL");
                model.CostoRiel = (await GetPMot("RIEL PARA CORTINERO MOTORIZADO")) * model.Riel;
                model.CarroMaestro = (model.TipoApertura == "DOS HOJAS") ? 2 : 1;

                string conceptoCarro = model.CarroMaestro == 1
                            ? "CARRO MAESTRO 1HOJA RIPP MOTOR"
                            : "CARRO MAESTRO 2HOJA RIPP MOTOR";

                model.CostoCarroMaestro = (await GetPMot(conceptoCarro)) * (decimal)model.CarroMaestro;

                model.CostoTopeCarritoSujecion = (await GetPMot("TOPE DE CARRITOS SUJECION")) * (decimal)model.TopeCarritoSujecion;
                model.CostoTopeMecanismoCortinero = (await GetPMot("TOPE MECANISMO DE CORTINERO")) * (decimal)model.TopeMecanismoCortinero;

                // Aquí usamos CARRITO PARA CORTINERO TRAD para Francesa
                // Buscamos el precio y lo asignamos a la variable que usaste para Francesa
                model.CostoCarritoCortinero = (await GetPMot("CARRITO PARA CORTINERO TRAD")) * model.CarritoCortinero;

                // ASIGNA EL VALOR TAMBIÉN A ESTA VARIABLE PARA QUE NO TE SALGA 0 EN LA BD
                model.CostoCarritos = model.CostoCarritoCortinero;
                model.CostoGanchoFinal = (await GetPMot("GANCHO FINAL")) * (decimal)model.GanchoFinal;

                model.CostoSoportes = (await GetPMot(inst.Contains("TECHO") ? "SOPORTE A TECHO" : "SOPORTE PARED")) * (decimal)model.Soportes;
                model.CostoEngrane = (await GetPMot("ENGRANE CORTINERO W")) * (decimal)model.Engrane;
                model.CostoUnionRiel = (await GetPMot("CONECTOR PERF - UNION")) * (decimal)model.UnionRiel;
                model.CostoHebilla = (await GetPMot("HEBILLA PARA CINTA CORTINERO RIPP")) * (decimal)model.Hebilla;
                model.CostoCorreaGoma = (await GetPMot("CORREA DE GOMA")) * (decimal)model.CorrederaGoma;
                model.CostoGomaVerde = (await GetPMot("GOMA VERDE")) * model.GomaVerde;
                model.CostoPesaPlomo = (await GetPMot("PESO PLOMO")) * (decimal)model.PesaPlomo;
                model.CostoTarlatana = (await GetPMot("TARLATANA")) * model.Tarlatana;
                model.CostoEmpaque = 20.00m;

                // SUMA TOTAL CORTINERO (Motor + Riel + Carros + Topes + Engranes + Hebillas + Gomas + Unión)
                model.CostoTotalCortinero = model.Motor + model.CostoRiel + model.CostoCarroMaestro +
                                            model.CostoTopeCarritoSujecion + model.CostoTopeMecanismoCortinero +
                                            model.CostoCarritoCortinero + model.CostoSoportes + model.CostoEngrane +
                                            model.CostoUnionRiel +
                                            model.TapasEngrane + model.CostoCorreaGoma + model.CostoEmpaque + model.CostoPesaPlomo + model.CostoGomaVerde;

                // --- 4. COMERCIAL ---
                model.PrecioTelaML = tela.precio_ml_corte * 1.16m;
                model.CostoTotalTela = (decimal)model.MLComprar * model.PrecioTelaML;

                // COSTO TOTAL CORTINA (FRANCESA): Tela + Tarlatana + Pesa Plomo + Empaque
                model.CostoTotalCortina = model.CostoTotalTela + model.CostoTarlatana + model.CostoGomaVerde;
                model.CostoTotalGeneral = model.CostoTotalCortinero + model.CostoTotalCortina;

                // Totales x3 y x2
                model.PrecioCortineroPublico = model.CostoTotalCortinero * 3;
                model.PrecioCortinaPublico = model.CostoTotalCortina * 3;
                model.TotalPublico = model.CostoTotalGeneral * 3;
                model.PrecioCortineroDistribuidor = model.CostoTotalCortinero * 2;
                model.PrecioCortinaDistribuidor = model.CostoTotalCortina * 2;
                model.TotalDistribuidor = model.CostoTotalGeneral * 2;
            }
            // ============================================================
            // BLOQUE 7: FRANCESA Y  MOTORIZADO BATERIA
            // ============================================================
            else if (tipoCortina == "FRANCESA" && sistema == "MOTORIZADO BATERIA")
            {
                // --- 1. INGENIERÍA FRANCESA ---
                // Carrito de cortinero se calcula igual que el carro embalado francés manual
                model.CarritoCortinero = (model.AnchoCM / 10m) - 1m;
                model.GanchoFinal = 2; // Siempre 2
                model.Ganchos = model.CarritoCortinero + 2m;


                // Tarlatana: ((CarritoCortinero * 10) + AnchoCM) / 100
                model.Tarlatana = ((model.CarritoCortinero * 10) + model.AnchoCM) / 100;

                model.TotalAnchoLienzo = model.Tarlatana + 0.52m;
                model.TotalAltura = (model.AlturaCM + 44) / 100;
                model.AlturaExacta = model.TotalAltura;
                model.NumLienzos = (int)Math.Ceiling(model.TotalAnchoLienzo / model.AnchoRollo);

                model.TotalML = model.AlturaExacta > model.AnchoRollo
                                ? (model.NumLienzos * model.AlturaExacta)
                                : model.TotalAnchoLienzo;
                model.MLComprar = (int)Math.Ceiling(model.TotalML);

                // --- 2. MECÁNICA MOTORIZADA (Igual que ondulada wifi) ---
                model.TopeCarritoSujecion = 1;
                model.TopeMecanismoCortinero = 1;
                model.Engrane = 2;
                model.TapasEngrane = 0;
                model.PesaPlomo = 4;
                model.Riel = model.Ancho;
                model.GomaVerde = model.Riel * 2.05m;
                model.UnionRiel = model.Ancho > 5.8m ? 1 : 0;
                model.CorrederaGoma = 2;

                string inst = (model.Instalacion ?? "MURO").ToUpper();
                var sopConfig = await _context.ConfigSoportes.FirstOrDefaultAsync(s => model.Ancho >= s.AnchoMin && model.Ancho <= s.AnchoMax);
                model.Soportes = inst.Contains("TECHO") ? 0 : (sopConfig?.CantidadSoportes ?? 0);

                // --- 3. COSTOS (SISTEMA MOTORIZADO) ---
                async Task<decimal> GetPMot(string concepto)
                {
                    var m = await _context.CostosMateriales.FirstOrDefaultAsync(x => x.concepto.ToUpper() == concepto.ToUpper() && x.sistema.ToUpper() == "MOTORIZADO");
                    return (m?.precio_unitario ?? 0m) * 1.16m;
                }

                model.Motor = await GetPMot("MOTOR BATERIAS + CONTROL + CARGADOR");
                model.CostoRiel = (await GetPMot("RIEL PARA CORTINERO MOTORIZADO")) * model.Riel;
                model.CarroMaestro = (model.TipoApertura == "DOS HOJAS") ? 2 : 1;

                string conceptoCarro = model.CarroMaestro == 1
                            ? "CARRO MAESTRO 1HOJA RIPP MOTOR"
                            : "CARRO MAESTRO 2HOJA RIPP MOTOR";

                model.CostoCarroMaestro = (await GetPMot(conceptoCarro)) * (decimal)model.CarroMaestro;

                model.CostoTopeCarritoSujecion = (await GetPMot("TOPE DE CARRITOS SUJECION")) * (decimal)model.TopeCarritoSujecion;
                model.CostoTopeMecanismoCortinero = (await GetPMot("TOPE MECANISMO DE CORTINERO")) * (decimal)model.TopeMecanismoCortinero;

                // Aquí usamos CARRITO PARA CORTINERO TRAD para Francesa
                // Buscamos el precio y lo asignamos a la variable que usaste para Francesa
                model.CostoCarritoCortinero = (await GetPMot("CARRITO PARA CORTINERO TRAD")) * model.CarritoCortinero;

                // ASIGNA EL VALOR TAMBIÉN A ESTA VARIABLE PARA QUE NO TE SALGA 0 EN LA BD
                model.CostoCarritos = model.CostoCarritoCortinero;
                model.CostoGanchoFinal = (await GetPMot("GANCHO FINAL")) * (decimal)model.GanchoFinal;

                model.CostoSoportes = (await GetPMot(inst.Contains("TECHO") ? "SOPORTE A TECHO" : "SOPORTE PARED")) * (decimal)model.Soportes;
                model.CostoEngrane = (await GetPMot("ENGRANE CORTINERO W")) * (decimal)model.Engrane;
                model.CostoUnionRiel = (await GetPMot("CONECTOR PERF - UNION")) * (decimal)model.UnionRiel;
                model.CostoHebilla = (await GetPMot("HEBILLA PARA CINTA CORTINERO RIPP")) * (decimal)model.Hebilla;
                model.CostoCorreaGoma = (await GetPMot("CORREA DE GOMA")) * (decimal)model.CorrederaGoma;
                model.CostoGomaVerde = (await GetPMot("GOMA VERDE")) * model.GomaVerde;
                model.CostoPesaPlomo = (await GetPMot("PESO PLOMO")) * (decimal)model.PesaPlomo;
                model.CostoTarlatana = (await GetPMot("TARLATANA")) * model.Tarlatana;
                model.CostoEmpaque = 20.00m;

                // SUMA TOTAL CORTINERO (Motor + Riel + Carros + Topes + Engranes + Hebillas + Gomas + Unión)
                model.CostoTotalCortinero = model.Motor + model.CostoRiel + model.CostoCarroMaestro +
                                            model.CostoTopeCarritoSujecion + model.CostoTopeMecanismoCortinero +
                                            model.CostoCarritoCortinero + model.CostoSoportes + model.CostoEngrane +
                                            model.CostoUnionRiel +
                                            model.TapasEngrane + model.CostoCorreaGoma + model.CostoEmpaque + model.CostoPesaPlomo + model.CostoGomaVerde;

                // --- 4. COMERCIAL ---
                model.PrecioTelaML = tela.precio_ml_corte * 1.16m;
                model.CostoTotalTela = (decimal)model.MLComprar * model.PrecioTelaML;

                // COSTO TOTAL CORTINA (FRANCESA): Tela + Tarlatana + Pesa Plomo + Empaque
                model.CostoTotalCortina = model.CostoTotalTela + model.CostoTarlatana + model.CostoGomaVerde;
                model.CostoTotalGeneral = model.CostoTotalCortinero + model.CostoTotalCortina;

                // Totales x3 y x2
                model.PrecioCortineroPublico = model.CostoTotalCortinero * 3;
                model.PrecioCortinaPublico = model.CostoTotalCortina * 3;
                model.TotalPublico = model.CostoTotalGeneral * 3;
                model.PrecioCortineroDistribuidor = model.CostoTotalCortinero * 2;
                model.PrecioCortinaDistribuidor = model.CostoTotalCortina * 2;
                model.TotalDistribuidor = model.CostoTotalGeneral * 2;
            }
// ============================================================
// BLOQUE 8: OJILLOS Y MANUAL
// ============================================================
else if (tipoCortina == "OJILLOS" && sistema == "MANUAL")
{
    // --- 1. LÓGICA DE INGENIERÍA ---

    // CANTIDAD DE OJILLOS POR LIENZO (Ancho en cm / 7)
    decimal ojillosPorLienzoRaw = model.AnchoCM / 7m;
    model.CantidadOjillosPorLienzo = (int)Math.Floor(ojillosPorLienzoRaw);

    // TOTAL OJILLOS (Redondear al par inferior - Estilo Redondear.Menos de Excel)
    int totalSugerido = (int)Math.Floor(model.AnchoCM / 7m);
    model.TotalOjillos = (totalSugerido % 2 == 0) ? totalSugerido : totalSugerido - 1;

    // ANCHO TOTAL DE LIENZO: (Total anillos * 16) + (26 una hoja / 42 dos hojas) / 100
    decimal constanteHojas = (model.TipoApertura == "DOS HOJAS") ? 42m : 26m;
    model.TotalAnchoLienzo = ((model.TotalOjillos * 16m) + constanteHojas) / 100m;

    // ALTURA TOTAL: (Altura cm + 44) / 100
    model.TotalAltura = (model.AlturaCM + 44m) / 100m;
    model.AlturaExacta = model.TotalAltura;

    // ANCHO DE ROLLO (Desde tabla Telas)
    model.AnchoRollo = tela.ancho;

    // ML REALES (Condición de giro de tela)
    if (model.TotalAltura < model.AnchoRollo)
    {
        model.TotalML = model.TotalAnchoLienzo;
    }
    else
    {
        model.NumLienzos = (int)Math.Ceiling(model.TotalAnchoLienzo / model.AnchoRollo);
        model.TotalML = (model.TotalAnchoLienzo / model.AnchoRollo) * model.TotalAltura;
    }

    // ML A COMPRAR: Redondear siempre al mayor (Ceiling)
    model.MLComprar = (int)Math.Ceiling(model.TotalML);

    // MEDIDA DE CORTINERO TUBULAR Y SOPORTES
    model.MedidaTubular = model.Ancho;
    model.Soportes = (model.MedidaTubular > 3m) ? 4 : 2;

    // TARLATANA: Igual al ancho total de lienzo
    model.Tarlatana = model.TotalAnchoLienzo;

    // --- 2. OBTENCIÓN DE PRECIOS (PARCHE "AMBOS") ---

    async Task<decimal> PrecioMat(string concepto, string sistemaFiltro = "AMBOS")
    {
        // Buscamos específicamente el concepto que coincida con el sistema "AMBOS"
        var m = await _context.CostosMateriales
            .FirstOrDefaultAsync(x => x.concepto.ToUpper() == concepto.ToUpper() 
                                   && x.sistema.ToUpper() == sistemaFiltro.ToUpper());
        
        // Si no existe con "AMBOS", buscamos el concepto general como respaldo
        if (m == null)
            m = await _context.CostosMateriales.FirstOrDefaultAsync(x => x.concepto.ToUpper() == concepto.ToUpper());

        return (m?.precio_unitario ?? 0m) * 1.16m; // Precio con IVA
    }

    // Recuperamos precios unitarios con IVA
    decimal pTubular = await PrecioMat("CORTINERO TUBULAR 1\"", "AMBOS");
    decimal pSoporte = await PrecioMat("SOPORTE A MURO", "AMBOS");
    decimal pOjillos = await PrecioMat("OJILLOS", "AMBOS");
    decimal pTarlatana = await PrecioMat("TARLATANA", "AMBOS");

    // --- 3. ASIGNACIÓN DE COSTOS ---

    // CORTINERO (Tubular * Medida)
    model.CostoRiel = pTubular * model.MedidaTubular;

    // SOPORTES (Validar Instalación)
    string instSafe = (model.Instalacion ?? "MURO").ToUpper();
    if (instSafe.Contains("TECHO"))
    {
        model.Soportes = 0;
        model.CostoSoportes = 0m;
    }
    else
    {
        model.CostoSoportes = pSoporte * (decimal)model.Soportes;
    }

    // OTROS COMPONENTES
    model.CostoEmpaque = 20.00m;
    model.CostoOjillos = pOjillos * (decimal)model.TotalOjillos;
    model.CostoTarlatana = pTarlatana * model.Tarlatana;

    // COSTO TOTAL CORTINERO (Cortinero + Soportes + Empaque)
    model.CostoTotalCortinero = model.CostoRiel + model.CostoSoportes + model.CostoEmpaque;

    // COSTO TOTAL CORTINA
    model.PrecioTelaML = tela.precio_ml_corte * 1.16m;
    model.CostoTotalTela = (decimal)model.MLComprar * model.PrecioTelaML;
    
    // Suma: Empaque + Ojillos + Tarlatana + Tela
    model.CostoTotalCortina = model.CostoEmpaque + model.CostoOjillos + model.CostoTarlatana + model.CostoTotalTela;

    // --- 4. TOTALES GENERALES Y COMERCIALES ---

    model.CostoTotalGeneral = model.CostoTotalCortina + model.CostoTotalCortinero;

    // PÚBLICO (x3)
    model.PrecioCortineroPublico = model.CostoTotalCortinero * 3;
    model.PrecioCortinaPublico = model.CostoTotalCortina * 3;
    model.TotalPublico = model.CostoTotalGeneral * 3;

    // DISTRIBUIDOR (x2)
    model.PrecioCortineroDistribuidor = model.CostoTotalCortinero * 2;
    model.PrecioCortinaDistribuidor = model.CostoTotalCortina * 2;
    model.TotalDistribuidor = model.CostoTotalGeneral * 2;
}
            // ============================================================
            // BLOQUE 9: OJILLOS Y MOTORIZADO BATERIA
            // ============================================================
            // ============================================================
            // BLOQUE 10: OJILLOS Y MOTORIZADO WIFI
            // ============================================================
            else return BadRequest("Combinación no válida.");

            _context.Cotizaciones.Add(model);
            await _context.SaveChangesAsync();
            return Ok(model);
        }
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Cotizaciones>>> GetCotizaciones()
        {
            // Retorna la lista completa de cotizaciones ordenadas por la más reciente
            return await _context.Cotizaciones
                .OrderByDescending(c => c.Id)
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Cotizaciones>> GetCotizacion(int id)
        {
            // Busca una cotización específica por su ID primario
            var cotizacion = await _context.Cotizaciones.FindAsync(id);

            if (cotizacion == null)
            {
                return NotFound(new { mensaje = $"La cotización con ID {id} no existe." });
            }

            return cotizacion;
        }
        [HttpGet("buscar-cliente/{nombre}")]
        public async Task<ActionResult<IEnumerable<Cotizaciones>>> BuscarPorCliente(string nombre)
        {
            // Si el nombre es nulo o espacios, devolvemos lista vacía
            if (string.IsNullOrWhiteSpace(nombre))
            {
                return Ok(new List<Cotizaciones>());
            }

            // El Contains con ToUpper es correcto para ignorar mayúsculas/minúsculas
            var resultados = await _context.Cotizaciones
                .Where(c => c.Cliente.ToUpper().Contains(nombre.ToUpper()))
                .OrderByDescending(c => c.Id)
                .ToListAsync();

            // IMPORTANTE: Siempre devolvemos Ok.
            // Si resultados está vacío, enviará [], y el JS entrará al bloque de "No se encontraron"
            return Ok(resultados);
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> EditCotizacion(int id, [FromBody] Cotizaciones modelInput)
        {
           
            // 1. Buscar el registro original en la DB
            var model = await _context.Cotizaciones.FindAsync(id);

            if (model == null)
            {
                return NotFound(new { mensaje = "La cotización no existe." });
            }
            // --- REINICIO DE CAMPOS MOTORIZADOS ---
            model.Motor = 0m;

            // 2. Actualizar solo los campos que el usuario puede cambiar desde el Front
            model.IdTela = modelInput.IdTela;
            model.Ancho = modelInput.Ancho;
            model.Alto = modelInput.Alto;
            model.TipoCortina = modelInput.TipoCortina;
            model.Area = modelInput.Area;
            model.Sistema = modelInput.Sistema;
            model.Instalacion = modelInput.Instalacion;
            model.Acoplamiento = modelInput.Acoplamiento;
            model.PorcentajeOnda = modelInput.PorcentajeOnda;

            // 3. RE-CALCULAR TODO (Lógica copiada del POST)

            // Traer la tela actualizada (por si cambiaron el IdTela)
            var tela = await _context.Telas
                .Include(t => t.modelo!)
                .ThenInclude(m => m.marca!)
                .FirstOrDefaultAsync(t => t.id == model.IdTela);

            if (tela == null) return BadRequest("Tela no encontrada");

            string tipoCortina = model.TipoCortina?.ToUpper() ?? "";
            string sistema = model.Sistema?.ToUpper() ?? "";

            // --- CAMPOS COMUNES ---
            model.Catalogo = tela.catalogo;
            model.Marca = tela.modelo?.marca?.nombre ?? "N/A";
            model.Modelo = tela.modelo?.nombre ?? "N/A";
            model.tipo = tela.tipo;
            model.ColorSeleccionado = model.ColorSeleccionado;
            model.AnchoRollo = tela.ancho;
            model.AnchoCM = model.Ancho * 100;
            model.AlturaCM = model.Alto * 100;
            model.M2 = model.Ancho * model.Alto;
            model.TipoApertura = (model.Acoplamiento?.ToUpper() == "AMBOS") ? "DOS HOJAS" : "UNA HOJA";

            // Funciones auxiliares de costos (dentro del scope del método)
            async Task<decimal> PrecioMat(string c)
            {
                var m = await _context.CostosMateriales.FirstOrDefaultAsync(x => x.concepto.ToUpper() == c.ToUpper());
                return (m?.precio_unitario ?? 0m) * 1.16m;
            }
           

            // ============================================================
            // BLOQUE 1: ONDULADO Y MANUAL (Cálculo de Ingeniería y Costos)
            // ============================================================
            if (tipoCortina == "ONDULADO" && sistema == "MANUAL")
            {
                decimal factor = model.PorcentajeOnda == 60 ? (10.8m / 6.8m) :
                                 model.PorcentajeOnda == 80 ? (10.8m / 6.1m) : (10.8m / 4.5m);

                decimal brochesRaw = (model.AnchoCM * factor) / 10.8m;
                model.CantidadBroches = (int)Math.Ceiling(brochesRaw);
                if (model.CantidadBroches % 2 != 0) model.CantidadBroches++;

                model.CintaBroches = Math.Round(((model.CantidadBroches * 10.8m) + 5) / 100, 2);
                model.TotalAnchoLienzo = model.CintaBroches + 0.52m;
                model.TotalAltura = (model.AlturaCM + 44) / 100;
                model.AlturaExacta = model.TotalAltura;
                model.NumLienzos = (int)Math.Ceiling(model.TotalAnchoLienzo / model.AnchoRollo);

                model.CarroMaestro = (model.TipoApertura == "DOS HOJAS") ? 2 : 1;
                model.Tapon = 2;
                model.Baston = model.CarroMaestro;
                model.CorrederasRipple = Math.Abs((model.CarroMaestro * 2) - model.CantidadBroches);

                model.TotalML = model.AlturaExacta > model.AnchoRollo ? (model.NumLienzos * model.AlturaExacta) : model.TotalAnchoLienzo;
                model.MLComprar = (int)Math.Ceiling(model.TotalML);
                model.Riel = model.Ancho;
                model.Tarlatana = model.CintaBroches;
                model.CintaPlomo = model.CintaBroches;
                model.UnionRiel = model.Ancho > 5.8m ? 1 : 0;

                string instSafe = (model.Instalacion ?? "MURO").ToUpper();
                if (instSafe.Contains("TECHO")) model.Soportes = 0;
                else
                {
                    var sop = await _context.ConfigSoportes.FirstOrDefaultAsync(s => model.Ancho >= s.AnchoMin && model.Ancho <= s.AnchoMax);
                    model.Soportes = sop?.CantidadSoportes ?? 0;
                }

                model.CostoRiel = (await PrecioMat("RIEL TECHO O MURO")) * model.Riel;
                model.CostoCarroMaestro = (await PrecioMat("CARRO MAESTRO IZQ - DER BROCHE DE METAL")) * (decimal)model.CarroMaestro;
                model.CostoCorredera = (await PrecioMat("CORREDERA DE 2 LLANTAS RIPPLEFOLD")) * (decimal)model.CorrederasRipple;
                model.CostoCintaBroches = (await PrecioMat("CINTA CON BROCHES")) * model.CintaBroches;
                model.CostoTapon = (await PrecioMat("TAPON RIPPLEFOLD")) * (decimal)model.Tapon;
                model.CostoBaston = (await PrecioMat("BASTON")) * (decimal)model.Baston;
                model.CostoSoportes = (await PrecioMat(instSafe.Contains("TECHO") ? "SOPORTE A TECHO" : "SOPORTE A MURO")) * (decimal)model.Soportes;
                model.CostoUnionRiel = model.UnionRiel > 0 ? (await PrecioMat("UNION PARA RIEL")) : 0m;
                model.CostoTarlatana = (await PrecioMat("TARLATANA")) * model.Tarlatana;
                model.CostoEmpaque = 20.00m;

                model.CostoTotalCortinero = model.CostoRiel + model.CostoCarroMaestro + model.CostoCorredera + model.CostoCintaBroches + model.CostoTapon + model.CostoBaston + model.CostoSoportes + model.CostoUnionRiel + model.CostoEmpaque;
                model.PrecioTelaML = tela.precio_ml_corte * 1.16m;
                model.CostoTotalTela = (decimal)model.MLComprar * model.PrecioTelaML;
                model.CostoTotalCortina = model.CostoTotalTela + model.CostoCintaBroches + model.CostoEmpaque + model.CostoTarlatana;
                model.CostoTotalGeneral = model.CostoTotalCortinero + model.CostoTotalCortina;

                model.PrecioCortineroPublico = model.CostoTotalCortinero * 3;
                model.PrecioCortinaPublico = model.CostoTotalCortina * 3;
                model.TotalPublico = model.CostoTotalGeneral * 3;
                model.PrecioCortineroDistribuidor = model.CostoTotalCortinero * 2;
                model.PrecioCortinaDistribuidor = model.CostoTotalCortina * 2;
                model.TotalDistribuidor = model.CostoTotalGeneral * 2;
            }
            // ============================================================
            // BLOQUE 2: ONDULADO Y MOTORIZADO WIFI
            // ============================================================
            else if (tipoCortina == "ONDULADO" && sistema == "MOTORIZADO WIFI")
            {
                // --- 1. INGENIERÍA ---
                decimal factor = model.PorcentajeOnda == 60 ? (10.8m / 6.8m) :
                                model.PorcentajeOnda == 80 ? (10.8m / 6.1m) : (10.8m / 4.5m);

                decimal brochesRaw = (model.AnchoCM * factor) / 10.8m;
                model.CantidadBroches = (int)Math.Ceiling(brochesRaw);
                if (model.CantidadBroches % 2 != 0) model.CantidadBroches++;

                model.CintaBroches = Math.Round(((model.CantidadBroches * 10.8m) + 5) / 100, 2);
                model.TotalAnchoLienzo = model.CintaBroches + 0.52m;
                model.TotalAltura = (model.AlturaCM + 44) / 100;
                model.AlturaExacta = model.TotalAltura;
                model.NumLienzos = (int)Math.Ceiling(model.TotalAnchoLienzo / model.AnchoRollo);
                model.TotalML = model.AlturaExacta > model.AnchoRollo ? (model.NumLienzos * model.AlturaExacta) : model.TotalAnchoLienzo;
                model.MLComprar = (int)Math.Ceiling(model.TotalML);

                // Piezas Fijas
                model.TopeCarritoSujecion = 1;
                model.TopeMecanismoCortinero = 1;
                model.Engrane = 2;
                model.TapasEngrane = 2;
                model.PesaPlomo = 4;
                model.Riel = model.Ancho;
                model.GomaVerde = (model.Ancho * 2) + 0.3m;
                model.UnionRiel = model.Ancho > 5.8m ? 1 : 0;

                if (model.TipoApertura == "DOS HOJAS")
                {
                    model.Hebilla = 2; model.CorrederaGoma = 2; model.CarroMaestro = 2;
                }
                else
                {
                    model.Hebilla = 1; model.CorrederaGoma = 1; model.CarroMaestro = 1;
                }

                // Soportes
                string inst = (model.Instalacion ?? "MURO").ToUpper();
                var sopConfig = await _context.ConfigSoportes.FirstOrDefaultAsync(s => model.Ancho >= s.AnchoMin && model.Ancho <= s.AnchoMax);
                model.Soportes = inst.Contains("TECHO") ? 0 : (sopConfig?.CantidadSoportes ?? 0);

                // --- 2. COSTOS (SISTEMA MOTORIZADO) ---
                async Task<decimal> GetP(string concepto)
                {
                    var m = await _context.CostosMateriales.FirstOrDefaultAsync(x => x.concepto.ToUpper() == concepto.ToUpper() && x.sistema.ToUpper() == "MOTORIZADO");
                    return (m?.precio_unitario ?? 0m) * 1.16m;
                }

                model.Motor = await GetP("MOTOR WIFI + CONTROL");
                model.CostoRiel = (await GetP("RIEL PARA CORTINERO MOTORIZADO")) * model.Riel;

                // Carro Maestro dinámico
                string conceptoCarro = (model.CarroMaestro == 2) ? "CARRO MAESTRO 2HOJA RIPP MOTOR" : "CARRO MAESTRO 1HOJA RIPP MOTOR";
                model.CostoCarroMaestro = (await GetP(conceptoCarro)) * (decimal)model.CarroMaestro;

                model.CostoTopeCarritoSujecion = (await GetP("TOPE DE CARRITOS SUJECION")) * (decimal)model.TopeCarritoSujecion;
                model.CostoTopeMecanismoCortinero = (await GetP("TOPE MECANISMO DE CORTINERO")) * (decimal)model.TopeMecanismoCortinero;
                model.CostoCarritos = (await GetP("CARRITO PARA CORTINERO ONDULADO")) * (decimal)model.CantidadBroches;
                model.CostoSoportes = (await GetP(inst.Contains("TECHO") ? "SOPORTE A TECHO" : "SOPORTE PARED")) * (decimal)model.Soportes;
                model.CostoEngrane = (await GetP("ENGRANE CORTINERO W")) * (decimal)model.Engrane;
                model.CostoUnionRiel = (await GetP("CONECTOR PERF - UNION")) * (decimal)model.UnionRiel;
                model.CostoTapasEngrane = (await GetP("TAPA PARA SISTEMA DE ENGRANE")) * (decimal)model.TapasEngrane;
                model.CostoHebilla = (await GetP("HEBILLA PARA CINTA CORTINERO RIPP")) * (decimal)model.Hebilla;
                model.CostoCorreaGoma = (await GetP("CORREA DE GOMA")) * (decimal)model.CorrederaGoma;
                model.CostoCintaBroches = (await GetP("CINTA CON BROCHES")) * model.CintaBroches;
                model.CostoGomaVerde = (await GetP("GOMA VERDE")) * model.GomaVerde;
                model.CostoPesaPlomo = (await GetP("PESO PLOMO")) * (decimal)model.PesaPlomo;
                model.CostoEmpaque = 20.00m;

                // SUMA TOTAL CORTINERO
                model.CostoTotalCortinero = model.Motor + model.CostoRiel + model.CostoCarroMaestro + model.CostoTopeCarritoSujecion +
                                            model.CostoTopeMecanismoCortinero + model.CostoCarritos + model.CostoSoportes +
                                            model.CostoEngrane + model.CostoUnionRiel + model.CostoTapasEngrane +
                                            model.CostoHebilla + model.CostoCorreaGoma + model.CostoEmpaque +
                                            model.CostoCintaBroches + model.CostoPesaPlomo + model.CostoGomaVerde;

                // --- 3. COMERCIAL ---
                model.PrecioTelaML = tela.precio_ml_corte * 1.16m;
                model.CostoTotalTela = (decimal)model.MLComprar * model.PrecioTelaML;

                // COSTO TOTAL CORTINA: Tela + Pesa Plomo + Cinta Broches + Empaque
                model.CostoTotalCortina = model.CostoTotalTela + model.CostoPesaPlomo + model.CostoCintaBroches + model.CostoEmpaque;
                model.CostoTotalGeneral = model.CostoTotalCortinero + model.CostoTotalCortina;

                // Totales x3 y x2
                model.PrecioCortineroPublico = model.CostoTotalCortinero * 3;
                model.PrecioCortinaPublico = model.CostoTotalCortina * 3;
                model.TotalPublico = model.CostoTotalGeneral * 3;
                model.PrecioCortineroDistribuidor = model.CostoTotalCortinero * 2;
                model.PrecioCortinaDistribuidor = model.CostoTotalCortina * 2;
                model.TotalDistribuidor = model.CostoTotalGeneral * 2;
            }

            // ============================================================
            // BLOQUE 3: ONDULADO Y MOTORIZADO BATERIA
            // ============================================================
            else if (tipoCortina == "ONDULADO" && sistema == "MOTORIZADO BATERIA")
            {
                // --- 1. INGENIERÍA ---
                decimal factor = model.PorcentajeOnda == 60 ? (10.8m / 6.8m) :
                                model.PorcentajeOnda == 80 ? (10.8m / 6.1m) : (10.8m / 4.5m);

                decimal brochesRaw = (model.AnchoCM * factor) / 10.8m;
                model.CantidadBroches = (int)Math.Ceiling(brochesRaw);
                if (model.CantidadBroches % 2 != 0) model.CantidadBroches++;

                model.CintaBroches = Math.Round(((model.CantidadBroches * 10.8m) + 5) / 100, 2);
                model.TotalAnchoLienzo = model.CintaBroches + 0.52m;
                model.TotalAltura = (model.AlturaCM + 44) / 100;
                model.AlturaExacta = model.TotalAltura;
                model.NumLienzos = (int)Math.Ceiling(model.TotalAnchoLienzo / model.AnchoRollo);
                model.TotalML = model.AlturaExacta > model.AnchoRollo ? (model.NumLienzos * model.AlturaExacta) : model.TotalAnchoLienzo;
                model.MLComprar = (int)Math.Ceiling(model.TotalML);

                // Piezas Fijas
                model.TopeCarritoSujecion = 1;
                model.TopeMecanismoCortinero = 1;
                model.Engrane = 2;
                model.TapasEngrane = 2;
                model.PesaPlomo = 4;
                model.Riel = model.Ancho;
                model.GomaVerde = (model.Ancho * 2) + 0.3m;
                model.UnionRiel = model.Ancho > 5.8m ? 1 : 0;

                if (model.TipoApertura == "DOS HOJAS")
                {
                    model.Hebilla = 2; model.CorrederaGoma = 2; model.CarroMaestro = 2;
                }
                else
                {
                    model.Hebilla = 1; model.CorrederaGoma = 1; model.CarroMaestro = 1;
                }

                // Soportes
                string inst = (model.Instalacion ?? "MURO").ToUpper();
                var sopConfig = await _context.ConfigSoportes.FirstOrDefaultAsync(s => model.Ancho >= s.AnchoMin && model.Ancho <= s.AnchoMax);
                model.Soportes = inst.Contains("TECHO") ? 0 : (sopConfig?.CantidadSoportes ?? 0);

                // --- 2. COSTOS (SISTEMA MOTORIZADO) ---
                async Task<decimal> GetP(string concepto)
                {
                    var m = await _context.CostosMateriales.FirstOrDefaultAsync(x => x.concepto.ToUpper() == concepto.ToUpper() && x.sistema.ToUpper() == "MOTORIZADO");
                    return (m?.precio_unitario ?? 0m) * 1.16m;
                }

                model.Motor = await GetP("MOTOR BATERIAS + CONTROL + CARGADOR");
                model.CostoRiel = (await GetP("RIEL PARA CORTINERO MOTORIZADO")) * model.Riel;

                // Carro Maestro dinámico
                string conceptoCarro = (model.CarroMaestro == 2) ? "CARRO MAESTRO 2HOJA RIPP MOTOR" : "CARRO MAESTRO 1HOJA RIPP MOTOR";
                model.CostoCarroMaestro = (await GetP(conceptoCarro)) * (decimal)model.CarroMaestro;

                model.CostoTopeCarritoSujecion = (await GetP("TOPE DE CARRITOS SUJECION")) * (decimal)model.TopeCarritoSujecion;
                model.CostoTopeMecanismoCortinero = (await GetP("TOPE MECANISMO DE CORTINERO")) * (decimal)model.TopeMecanismoCortinero;
                model.CostoCarritos = (await GetP("CARRITO PARA CORTINERO ONDULADO")) * (decimal)model.CantidadBroches;
                model.CostoSoportes = (await GetP(inst.Contains("TECHO") ? "SOPORTE A TECHO" : "SOPORTE PARED")) * (decimal)model.Soportes;
                model.CostoEngrane = (await GetP("ENGRANE CORTINERO W")) * (decimal)model.Engrane;
                model.CostoUnionRiel = (await GetP("CONECTOR PERF - UNION")) * (decimal)model.UnionRiel;
                model.CostoTapasEngrane = (await GetP("TAPA PARA SISTEMA DE ENGRANE")) * (decimal)model.TapasEngrane;
                model.CostoHebilla = (await GetP("HEBILLA PARA CINTA CORTINERO RIPP")) * (decimal)model.Hebilla;
                model.CostoCorreaGoma = (await GetP("CORREA DE GOMA")) * (decimal)model.CorrederaGoma;
                model.CostoCintaBroches = (await GetP("CINTA CON BROCHES")) * model.CintaBroches;
                model.CostoGomaVerde = (await GetP("GOMA VERDE")) * model.GomaVerde;
                model.CostoPesaPlomo = (await GetP("PESO PLOMO")) * (decimal)model.PesaPlomo;
                model.CostoEmpaque = 20.00m;

                // SUMA TOTAL CORTINERO
                model.CostoTotalCortinero = model.Motor + model.CostoRiel + model.CostoCarroMaestro + model.CostoTopeCarritoSujecion +
                                            model.CostoTopeMecanismoCortinero + model.CostoCarritos + model.CostoSoportes +
                                            model.CostoEngrane + model.CostoUnionRiel + model.CostoTapasEngrane +
                                            model.CostoHebilla + model.CostoCorreaGoma + model.CostoEmpaque +
                                            model.CostoCintaBroches + model.CostoPesaPlomo + model.CostoGomaVerde;

                // --- 3. COMERCIAL ---
                model.PrecioTelaML = tela.precio_ml_corte * 1.16m;
                model.CostoTotalTela = (decimal)model.MLComprar * model.PrecioTelaML;

                // COSTO TOTAL CORTINA: Tela + Pesa Plomo + Cinta Broches + Empaque
                model.CostoTotalCortina = model.CostoTotalTela + model.CostoPesaPlomo + model.CostoCintaBroches + model.CostoEmpaque;
                model.CostoTotalGeneral = model.CostoTotalCortinero + model.CostoTotalCortina;

                // Totales x3 y x2
                model.PrecioCortineroPublico = model.CostoTotalCortinero * 3;
                model.PrecioCortinaPublico = model.CostoTotalCortina * 3;
                model.TotalPublico = model.CostoTotalGeneral * 3;
                model.PrecioCortineroDistribuidor = model.CostoTotalCortinero * 2;
                model.PrecioCortinaDistribuidor = model.CostoTotalCortina * 2;
                model.TotalDistribuidor = model.CostoTotalGeneral * 2;
            }
            // ============================================================
            // BLOQUE 4: FRANCESA Y MANUAL
            // ============================================================
            else if (tipoCortina == "FRANCESA" && sistema == "MANUAL")
            {
                model.CarroEmbaladoFrances = (int)Math.Floor(model.AnchoCM / 10) - 1;
                model.Tarlatana = ((model.CarroEmbaladoFrances * 10) + model.AnchoCM + 10) / 100;
                model.TotalAnchoLienzo = model.Tarlatana + 0.52m;
                model.TotalAltura = (model.AlturaCM + 44) / 100;
                model.AlturaExacta = model.TotalAltura;
                model.NumLienzos = (int)Math.Ceiling(model.TotalAnchoLienzo / model.AnchoRollo);

                model.TotalML = model.AlturaExacta > model.AnchoRollo ? (model.NumLienzos * model.AlturaExacta) : model.TotalAnchoLienzo;
                model.MLComprar = (int)Math.Ceiling(model.TotalML);
                model.Riel = model.Ancho;
                model.CarroMaestro = (model.TipoApertura == "DOS HOJAS") ? 2 : 1;
                model.Tapon = 2;
                model.Ganchos = model.CarroEmbaladoFrances + 2;
                model.Baston = model.CarroMaestro;
                model.UnionRiel = model.Ancho > 5.8m ? 1 : 0;

                string inst = (model.Instalacion ?? "MURO").ToUpper();
                if (inst.Contains("TECHO")) model.Soportes = 0;
                else
                {
                    var sop = await _context.ConfigSoportes.FirstOrDefaultAsync(s => model.Ancho >= s.AnchoMin && model.Ancho <= s.AnchoMax);
                    model.Soportes = sop?.CantidadSoportes ?? 0;
                }

                async Task<decimal> PrecioMatIva(string c)
                {
                    var m = await _context.CostosMateriales.FirstOrDefaultAsync(x => x.concepto.ToUpper() == c);
                    return (m?.precio_unitario ?? 0m) * 1.16m;
                }

                model.CostoRiel = (await PrecioMatIva("RIEL TECHO O MURO")) * model.Riel;
                model.CostoCarroMaestro = (await PrecioMatIva("CARRO MAESTRO IZQ - DER FRANCES")) * (decimal)model.CarroMaestro;
                model.CostoCarroEmbaladoFrances = (await PrecioMatIva("CARRO EMBALADO FRANCES")) * (decimal)model.CarroEmbaladoFrances;
                model.CostoTapon = (await PrecioMatIva("TAPON FRANCES")) * (decimal)model.Tapon;
                model.CostoGanchos = (await PrecioMatIva("GANCHOS METALICOS")) * (decimal)model.Ganchos;
                model.CostoBaston = (await PrecioMatIva("BASTON")) * (decimal)model.Baston;
                model.CostoSoportes = (await PrecioMatIva(inst.Contains("TECHO") ? "SOPORTE A TECHO" : "SOPORTE A MURO")) * (decimal)model.Soportes;
                model.CostoUnionRiel = model.UnionRiel > 0 ? (await PrecioMatIva("UNION PARA RIEL")) : 0m;
                model.CostoTarlatana = (await PrecioMatIva("TARLATANA")) * model.Tarlatana;
                model.CostoEmpaque = 20.00m;

                model.CostoTotalCortinero = model.CostoRiel + model.CostoCarroMaestro + model.CostoCarroEmbaladoFrances + model.CostoTapon + model.CostoGanchos + model.CostoBaston + model.CostoSoportes + model.CostoUnionRiel + model.CostoEmpaque;

                model.PrecioTelaML = tela.precio_ml_corte * 1.16m;

                model.CostoTotalTela = (decimal)model.MLComprar * model.PrecioTelaML;
                model.CostoTotalCortina = model.CostoTotalTela + model.CostoTarlatana + model.CostoEmpaque;
                model.CostoTotalGeneral = model.CostoTotalCortinero + model.CostoTotalCortina;

                model.PrecioCortineroPublico = model.CostoTotalCortinero * 3;
                model.PrecioCortinaPublico = model.CostoTotalCortina * 3;
                model.TotalPublico = model.CostoTotalGeneral * 3;
                model.PrecioCortineroDistribuidor = model.CostoTotalCortinero * 2;
                model.PrecioCortinaDistribuidor = model.CostoTotalCortina * 2;
                model.TotalDistribuidor = model.CostoTotalGeneral * 2;
            }
            // ============================================================
            // BLOQUE 5: FRANCESA Y MOTORIZADO ESTANDAR
            // ============================================================
            else if (tipoCortina == "FRANCESA" && sistema == "MOTORIZADO ESTANDAR")
            {
                // --- 1. INGENIERÍA (Igual que la manual) ---
                model.CarroEmbaladoFrances = (int)Math.Floor(model.AnchoCM / 10) - 1;
                model.Tarlatana = ((model.CarroEmbaladoFrances * 10) + model.AnchoCM + 10) / 100;
                model.TotalAnchoLienzo = model.Tarlatana + 0.52m;
                model.TotalAltura = (model.AlturaCM + 44) / 100;
                model.AlturaExacta = model.TotalAltura;
                model.NumLienzos = (int)Math.Ceiling(model.TotalAnchoLienzo / model.AnchoRollo);

                model.TotalML = model.AlturaExacta > model.AnchoRollo
                                ? (model.NumLienzos * model.AlturaExacta)
                                : model.TotalAnchoLienzo;

                model.MLComprar = (int)Math.Ceiling(model.TotalML);

                // --- 2. LÓGICA DE ACCESORIOS (Todo a 0 por ser motorizado) ---
                model.Riel = 0;
                model.CarroMaestro = 0;
                model.Tapon = 0;
                model.Ganchos = 0;
                model.Baston = 0;
                model.Soportes = 0;
                model.UnionRiel = 0;
                model.CarroEmbaladoFrances = 0;

                // --- 3. COSTOS (Valores en 0 y Costo Fijo) ---
                model.CostoRiel = 0;
                model.CostoCarroMaestro = 0;
                model.CostoCarroEmbaladoFrances = 0;
                model.CostoTapon = 0;
                model.CostoGanchos = 0;
                model.CostoBaston = 0;
                model.CostoSoportes = 0;
                model.CostoUnionRiel = 0;
                model.CostoTarlatana = 0; // Se suma en la cortina, no en el cortinero
                model.CostoEmpaque = 20.00m;

                // COSTO TOTAL CORTINERO FIJO
                model.CostoTotalCortinero = 2335.00m;

                // --- 4. CÁLCULOS COMERCIALES (TELA Y TOTALES) ---
                model.PrecioTelaML = tela.precio_ml_corte * 1.16m;
                model.CostoTotalTela = (decimal)model.MLComprar * model.PrecioTelaML;

                // Aquí calculamos la tarlatana para la cortina (necesitamos el precio con IVA)
                var matTarlatana = await _context.CostosMateriales.FirstOrDefaultAsync(x => x.concepto.ToUpper() == "TARLATANA");
                decimal precioTarlatanaIva = (matTarlatana?.precio_unitario ?? 0m) * 1.16m;
                model.CostoTarlatana = precioTarlatanaIva * model.Tarlatana;

                // Costo Total Cortina
                model.CostoTotalCortina = model.CostoTotalTela + model.CostoTarlatana + model.CostoEmpaque;
                model.CostoTotalGeneral = model.CostoTotalCortinero + model.CostoTotalCortina;

                // PRECIOS PÚBLICO (x3)
                model.PrecioCortineroPublico = model.CostoTotalCortinero * 3;
                model.PrecioCortinaPublico = model.CostoTotalCortina * 3;
                model.TotalPublico = model.CostoTotalGeneral * 3;

                // PRECIOS DISTRIBUIDOR (x2)
                model.PrecioCortineroDistribuidor = model.CostoTotalCortinero * 2;
                model.PrecioCortinaDistribuidor = model.CostoTotalCortina * 2;
                model.TotalDistribuidor = model.CostoTotalGeneral * 2;
            }
            // ============================================================
            // BLOQUE 6: FRANCESA Y MOTORIZADO WIFI
            // ============================================================
            else if (tipoCortina == "FRANCESA" && sistema == "MOTORIZADO WIFI")
            {
                // --- 1. INGENIERÍA FRANCESA ---
                // Carrito de cortinero se calcula igual que el carro embalado francés manual
                model.CarritoCortinero = (model.AnchoCM / 10m) - 1m;
                model.GanchoFinal = 2; // Siempre 2
                model.Ganchos = model.CarritoCortinero + 2m;


                // Tarlatana: ((CarritoCortinero * 10) + AnchoCM) / 100
                model.Tarlatana = ((model.CarritoCortinero * 10) + model.AnchoCM) / 100;

                model.TotalAnchoLienzo = model.Tarlatana + 0.52m;
                model.TotalAltura = (model.AlturaCM + 44) / 100;
                model.AlturaExacta = model.TotalAltura;
                model.NumLienzos = (int)Math.Ceiling(model.TotalAnchoLienzo / model.AnchoRollo);

                model.TotalML = model.AlturaExacta > model.AnchoRollo
                                ? (model.NumLienzos * model.AlturaExacta)
                                : model.TotalAnchoLienzo;
                model.MLComprar = (int)Math.Ceiling(model.TotalML);

                // --- 2. MECÁNICA MOTORIZADA (Igual que ondulada wifi) ---
                model.TopeCarritoSujecion = 1;
                model.TopeMecanismoCortinero = 1;
                model.Engrane = 2;
                model.TapasEngrane = 0;
                model.PesaPlomo = 4;
                model.Riel = model.Ancho;
                model.GomaVerde = model.Riel * 2.05m;
                model.UnionRiel = model.Ancho > 5.8m ? 1 : 0;
                model.CorrederaGoma = 2;

                string inst = (model.Instalacion ?? "MURO").ToUpper();
                var sopConfig = await _context.ConfigSoportes.FirstOrDefaultAsync(s => model.Ancho >= s.AnchoMin && model.Ancho <= s.AnchoMax);
                model.Soportes = inst.Contains("TECHO") ? 0 : (sopConfig?.CantidadSoportes ?? 0);

                // --- 3. COSTOS (SISTEMA MOTORIZADO) ---
                async Task<decimal> GetPMot(string concepto)
                {
                    var m = await _context.CostosMateriales.FirstOrDefaultAsync(x => x.concepto.ToUpper() == concepto.ToUpper() && x.sistema.ToUpper() == "MOTORIZADO");
                    return (m?.precio_unitario ?? 0m) * 1.16m;
                }

                model.Motor = await GetPMot("MOTOR WIFI + CONTROL");
                model.CostoRiel = (await GetPMot("RIEL PARA CORTINERO MOTORIZADO")) * model.Riel;
                model.CarroMaestro = (model.TipoApertura == "DOS HOJAS") ? 2 : 1;

                string conceptoCarro = model.CarroMaestro == 1
                            ? "CARRO MAESTRO 1HOJA RIPP MOTOR"
                            : "CARRO MAESTRO 2HOJA RIPP MOTOR";

                model.CostoCarroMaestro = (await GetPMot(conceptoCarro)) * (decimal)model.CarroMaestro;

                model.CostoTopeCarritoSujecion = (await GetPMot("TOPE DE CARRITOS SUJECION")) * (decimal)model.TopeCarritoSujecion;
                model.CostoTopeMecanismoCortinero = (await GetPMot("TOPE MECANISMO DE CORTINERO")) * (decimal)model.TopeMecanismoCortinero;

                // Aquí usamos CARRITO PARA CORTINERO TRAD para Francesa
                // Buscamos el precio y lo asignamos a la variable que usaste para Francesa
                model.CostoCarritoCortinero = (await GetPMot("CARRITO PARA CORTINERO TRAD")) * model.CarritoCortinero;

                // ASIGNA EL VALOR TAMBIÉN A ESTA VARIABLE PARA QUE NO TE SALGA 0 EN LA BD
                model.CostoCarritos = model.CostoCarritoCortinero;
                model.CostoGanchoFinal = (await GetPMot("GANCHO FINAL")) * (decimal)model.GanchoFinal;

                model.CostoSoportes = (await GetPMot(inst.Contains("TECHO") ? "SOPORTE A TECHO" : "SOPORTE PARED")) * (decimal)model.Soportes;
                model.CostoEngrane = (await GetPMot("ENGRANE CORTINERO W")) * (decimal)model.Engrane;
                model.CostoUnionRiel = (await GetPMot("CONECTOR PERF - UNION")) * (decimal)model.UnionRiel;
                model.CostoHebilla = (await GetPMot("HEBILLA PARA CINTA CORTINERO RIPP")) * (decimal)model.Hebilla;
                model.CostoCorreaGoma = (await GetPMot("CORREA DE GOMA")) * (decimal)model.CorrederaGoma;
                model.CostoGomaVerde = (await GetPMot("GOMA VERDE")) * model.GomaVerde;
                model.CostoPesaPlomo = (await GetPMot("PESO PLOMO")) * (decimal)model.PesaPlomo;
                model.CostoTarlatana = (await GetPMot("TARLATANA")) * model.Tarlatana;
                model.CostoEmpaque = 20.00m;

                // SUMA TOTAL CORTINERO (Motor + Riel + Carros + Topes + Engranes + Hebillas + Gomas + Unión)
                model.CostoTotalCortinero = model.Motor + model.CostoRiel + model.CostoCarroMaestro +
                                            model.CostoTopeCarritoSujecion + model.CostoTopeMecanismoCortinero +
                                            model.CostoCarritoCortinero + model.CostoSoportes + model.CostoEngrane +
                                            model.CostoUnionRiel +
                                            model.TapasEngrane + model.CostoCorreaGoma + model.CostoEmpaque + model.CostoPesaPlomo + model.CostoGomaVerde;

                // --- 4. COMERCIAL ---
                model.PrecioTelaML = tela.precio_ml_corte * 1.16m;
                model.CostoTotalTela = (decimal)model.MLComprar * model.PrecioTelaML;

                // COSTO TOTAL CORTINA (FRANCESA): Tela + Tarlatana + Pesa Plomo + Empaque
                model.CostoTotalCortina = model.CostoTotalTela + model.CostoTarlatana + model.CostoGomaVerde;
                model.CostoTotalGeneral = model.CostoTotalCortinero + model.CostoTotalCortina;

                // Totales x3 y x2
                model.PrecioCortineroPublico = model.CostoTotalCortinero * 3;
                model.PrecioCortinaPublico = model.CostoTotalCortina * 3;
                model.TotalPublico = model.CostoTotalGeneral * 3;
                model.PrecioCortineroDistribuidor = model.CostoTotalCortinero * 2;
                model.PrecioCortinaDistribuidor = model.CostoTotalCortina * 2;
                model.TotalDistribuidor = model.CostoTotalGeneral * 2;
            }
            // ============================================================
            // BLOQUE 7: FRANCESA Y  MOTORIZADO BATERIA
            // ============================================================
            else if (tipoCortina == "FRANCESA" && sistema == "MOTORIZADO BATERIA")
            {
                // --- 1. INGENIERÍA FRANCESA ---
                // Carrito de cortinero se calcula igual que el carro embalado francés manual
                model.CarritoCortinero = (model.AnchoCM / 10m) - 1m;
                model.GanchoFinal = 2; // Siempre 2
                model.Ganchos = model.CarritoCortinero + 2m;


                // Tarlatana: ((CarritoCortinero * 10) + AnchoCM) / 100
                model.Tarlatana = ((model.CarritoCortinero * 10) + model.AnchoCM) / 100;

                model.TotalAnchoLienzo = model.Tarlatana + 0.52m;
                model.TotalAltura = (model.AlturaCM + 44) / 100;
                model.AlturaExacta = model.TotalAltura;
                model.NumLienzos = (int)Math.Ceiling(model.TotalAnchoLienzo / model.AnchoRollo);

                model.TotalML = model.AlturaExacta > model.AnchoRollo
                                ? (model.NumLienzos * model.AlturaExacta)
                                : model.TotalAnchoLienzo;
                model.MLComprar = (int)Math.Ceiling(model.TotalML);

                // --- 2. MECÁNICA MOTORIZADA (Igual que ondulada wifi) ---
                model.TopeCarritoSujecion = 1;
                model.TopeMecanismoCortinero = 1;
                model.Engrane = 2;
                model.TapasEngrane = 0;
                model.PesaPlomo = 4;
                model.Riel = model.Ancho;
                model.GomaVerde = model.Riel * 2.05m;
                model.UnionRiel = model.Ancho > 5.8m ? 1 : 0;
                model.CorrederaGoma = 2;

                string inst = (model.Instalacion ?? "MURO").ToUpper();
                var sopConfig = await _context.ConfigSoportes.FirstOrDefaultAsync(s => model.Ancho >= s.AnchoMin && model.Ancho <= s.AnchoMax);
                model.Soportes = inst.Contains("TECHO") ? 0 : (sopConfig?.CantidadSoportes ?? 0);

                // --- 3. COSTOS (SISTEMA MOTORIZADO) ---
                async Task<decimal> GetPMot(string concepto)
                {
                    var m = await _context.CostosMateriales.FirstOrDefaultAsync(x => x.concepto.ToUpper() == concepto.ToUpper() && x.sistema.ToUpper() == "MOTORIZADO");
                    return (m?.precio_unitario ?? 0m) * 1.16m;
                }

                model.Motor = await GetPMot("MOTOR BATERIAS + CONTROL + CARGADOR");
                model.CostoRiel = (await GetPMot("RIEL PARA CORTINERO MOTORIZADO")) * model.Riel;
                model.CarroMaestro = (model.TipoApertura == "DOS HOJAS") ? 2 : 1;

                string conceptoCarro = model.CarroMaestro == 1
                            ? "CARRO MAESTRO 1HOJA RIPP MOTOR"
                            : "CARRO MAESTRO 2HOJA RIPP MOTOR";

                model.CostoCarroMaestro = (await GetPMot(conceptoCarro)) * (decimal)model.CarroMaestro;

                model.CostoTopeCarritoSujecion = (await GetPMot("TOPE DE CARRITOS SUJECION")) * (decimal)model.TopeCarritoSujecion;
                model.CostoTopeMecanismoCortinero = (await GetPMot("TOPE MECANISMO DE CORTINERO")) * (decimal)model.TopeMecanismoCortinero;

                // Aquí usamos CARRITO PARA CORTINERO TRAD para Francesa
                // Buscamos el precio y lo asignamos a la variable que usaste para Francesa
                model.CostoCarritoCortinero = (await GetPMot("CARRITO PARA CORTINERO TRAD")) * model.CarritoCortinero;

                // ASIGNA EL VALOR TAMBIÉN A ESTA VARIABLE PARA QUE NO TE SALGA 0 EN LA BD
                model.CostoCarritos = model.CostoCarritoCortinero;
                model.CostoGanchoFinal = (await GetPMot("GANCHO FINAL")) * (decimal)model.GanchoFinal;

                model.CostoSoportes = (await GetPMot(inst.Contains("TECHO") ? "SOPORTE A TECHO" : "SOPORTE PARED")) * (decimal)model.Soportes;
                model.CostoEngrane = (await GetPMot("ENGRANE CORTINERO W")) * (decimal)model.Engrane;
                model.CostoUnionRiel = (await GetPMot("CONECTOR PERF - UNION")) * (decimal)model.UnionRiel;
                model.CostoHebilla = (await GetPMot("HEBILLA PARA CINTA CORTINERO RIPP")) * (decimal)model.Hebilla;
                model.CostoCorreaGoma = (await GetPMot("CORREA DE GOMA")) * (decimal)model.CorrederaGoma;
                model.CostoGomaVerde = (await GetPMot("GOMA VERDE")) * model.GomaVerde;
                model.CostoPesaPlomo = (await GetPMot("PESO PLOMO")) * (decimal)model.PesaPlomo;
                model.CostoTarlatana = (await GetPMot("TARLATANA")) * model.Tarlatana;
                model.CostoEmpaque = 20.00m;

                // SUMA TOTAL CORTINERO (Motor + Riel + Carros + Topes + Engranes + Hebillas + Gomas + Unión)
                model.CostoTotalCortinero = model.Motor + model.CostoRiel + model.CostoCarroMaestro +
                                            model.CostoTopeCarritoSujecion + model.CostoTopeMecanismoCortinero +
                                            model.CostoCarritoCortinero + model.CostoSoportes + model.CostoEngrane +
                                            model.CostoUnionRiel +
                                            model.TapasEngrane + model.CostoCorreaGoma + model.CostoEmpaque + model.CostoPesaPlomo + model.CostoGomaVerde;

                // --- 4. COMERCIAL ---
                model.PrecioTelaML = tela.precio_ml_corte * 1.16m;
                model.CostoTotalTela = (decimal)model.MLComprar * model.PrecioTelaML;

                // COSTO TOTAL CORTINA (FRANCESA): Tela + Tarlatana + Pesa Plomo + Empaque
                model.CostoTotalCortina = model.CostoTotalTela + model.CostoTarlatana + model.CostoGomaVerde;
                model.CostoTotalGeneral = model.CostoTotalCortinero + model.CostoTotalCortina;

                // Totales x3 y x2
                model.PrecioCortineroPublico = model.CostoTotalCortinero * 3;
                model.PrecioCortinaPublico = model.CostoTotalCortina * 3;
                model.TotalPublico = model.CostoTotalGeneral * 3;
                model.PrecioCortineroDistribuidor = model.CostoTotalCortinero * 2;
                model.PrecioCortinaDistribuidor = model.CostoTotalCortina * 2;
                model.TotalDistribuidor = model.CostoTotalGeneral * 2;
            }
            // ============================================================
            // BLOQUE 8: OJILLOS Y MANUAL
            // ============================================================
            // ============================================================
            // BLOQUE 9: OJILLOS Y MOTORIZADO BATERIA
            // ============================================================
            // ============================================================
            // BLOQUE 10: OJILLOS Y MOTORIZADO WIFI
            // ============================================================

            // 4. Guardar cambios en la base de datos
            try
            {
                model.PresupuestoId = modelInput.PresupuestoId;
                _context.Entry(model).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return BadRequest(new { mensaje = "Error de concurrencia al actualizar." });
            }

            return Ok(new { mensaje = "Cotización actualizada y recalculada con éxito", data = model });
        }
        // Clase de apoyo para recibir los datos del Front-end
        public class PresupuestoRequest
        {
            public string Nombre { get; set; } = string.Empty;
            public int UsuarioId { get; set; }
            public string? Numero { get; set; }
            public string? Direccion { get; set; }
        }

        [HttpPost("CrearPresupuesto")]
        public async Task<IActionResult> CrearPresupuesto([FromBody] PresupuestoRequest request)
        {
            // Validamos usando 'request.Nombre' porque así viene del JSON
            if (string.IsNullOrEmpty(request.Nombre) || request.Nombre == "string")
                return BadRequest("El nombre del cliente es obligatorio.");

            var p = new Presupuesto {
                NombreCliente = request.Nombre,
                Numero = request.Numero,       // Pasa del sobre (request) a la DB (p)
                Direccion = request.Direccion, // Pasa del sobre (request) a la DB (p)
                FechaCreacion = DateTime.Now,
                UsuarioId = request.UsuarioId 
            };

            _context.Presupuestos.Add(p);
            await _context.SaveChangesAsync();

            return Ok(new { 
                id = p.Id, 
                nombre = p.NombreCliente,
                mensaje = "Ya puedes agregar cortinas a este cliente" 
            });
        }
        [HttpGet("Reporte/{presupuestoId}")]
        public async Task<IActionResult> GetReporte(int presupuestoId)
        {
            var reporte = await _context.Presupuestos
           
                .Include(p => p.Cotizaciones) // Esto trae todas las cortinas asociadas
                .FirstOrDefaultAsync(p => p.Id == presupuestoId);

            if (reporte == null) return NotFound("No existe ese presupuesto");

            return Ok(reporte);
        }
        [HttpGet("PresupuestoCompleto/{id}")]
        public async Task<IActionResult> GetPresupuestoCompleto(int id)
        {
            var presupuesto = await _context.Presupuestos
                .Include(p => p.Cotizaciones) // Esto trae todas las cortinas asociadas        
                .FirstOrDefaultAsync(p => p.Id == id);

            if (presupuesto == null)
                return NotFound(new { mensaje = "Presupuesto no encontrado" });

            presupuesto.TotalPresupuesto = presupuesto.Cotizaciones.Sum(c => c.TotalPublico);

            return Ok(new {
                cliente = presupuesto.NombreCliente,
                numero = presupuesto.Numero,
                direccion = presupuesto.Direccion,
                fecha = presupuesto.FechaCreacion,
                totalGlobal = presupuesto.TotalPresupuesto,
                observaciones = presupuesto.Observaciones,
                detalleCortinas = presupuesto.Cotizaciones.Select(c => new {
                    // 1. DATOS GENERALES Y DISEÑO
                    c.Id,
                    c.Area,
                    c.Cliente,
                    c.TipoCortina,
                    c.Sistema,
                    c.Modelo,
                    c.Marca,
                    c.Catalogo,
                    c.tipo,
                    c.Acoplamiento,
                    c.TipoApertura,
                    c.Instalacion,

                    // 2. MEDIDAS Y ÁREAS
                    c.Ancho,
                    c.Alto,
                    c.M2,
                    c.AnchoCM,
                    c.AlturaCM,
                    c.AnchoRollo,

                    // 3. INGENIERÍA Y COMPONENTES (Cantidades)
                    c.PorcentajeOnda,
                    c.CantidadBroches,
                    c.CintaBroches,
                    c.TotalAnchoLienzo,
                    c.TotalAltura,
                    c.AlturaExacta,
                    c.CintaPlomo,
                    c.CarroMaestro,
                    c.CorrederasRipple,
                    c.NumLienzos,
                    c.TotalML,
                    c.MLComprar,
                    c.Riel,
                    c.Tapon,
                    c.Baston,
                    c.Soportes,
                    c.UnionRiel,
                    c.Tarlatana,
                    c.CarroEmbaladoFrances,
                    c.Ganchos,
                    c.TopeCarritoSujecion,
                    c.TopeMecanismoCortinero,
                    c.Engrane,
                    c.TapasEngrane,
                    c.Hebilla,
                    c.CorrederaGoma,
                    c.GomaVerde,
                    c.PesaPlomo,
                    c.CarritoCortinero,
                    c.GanchoFinal,

                    // 4. COSTOS DE COMPONENTES (Precios Unitarios/Totales Material)
                    c.CostoRiel,
                    c.CostoCarroMaestro,
                    c.CostoCorredera,
                    c.CostoCintaBroches,
                    c.CostoTapon,
                    c.CostoBaston,
                    c.CostoSoportes,
                    c.CostoUnionRiel,
                    c.CostoEmpaque,
                    c.CostoTarlatana,
                    c.CostoCarroEmbaladoFrances,
                    c.CostoGanchos,
                    c.CostoTopeCarritoSujecion,
                    c.CostoTopeMecanismoCortinero,
                    c.CostoEngrane,
                    c.CostoTapasEngrane,
                    c.CostoHebilla,
                    c.CostoCorreaGoma,
                    c.CostoGomaVerde,
                    c.CostoPesaPlomo,
                    c.CostoCarritos,
                    c.CostoCarritoCortinero,
                    c.CostoGanchoFinal,
                    c.Motor,
                    c.PrecioTelaML,
                    c.CostoTotalTela,

                    // 5. TOTALES DE COSTO INTERNO
                    c.CostoTotalCortinero,
                    c.CostoTotalCortina,
                    c.CostoTotalGeneral,

                    // 6. PRECIOS DE VENTA (PÚBLICO)
                    c.PrecioCortineroPublico,
                    c.PrecioCortinaPublico,
                    c.TotalPublico,

                    // 7. PRECIOS DE VENTA (DISTRIBUIDOR)
                    c.PrecioCortineroDistribuidor,
                    c.PrecioCortinaDistribuidor,
                    c.TotalDistribuidor,
                   
                    // Campo de validación
                    totalPartidaVerificacion = c.TotalPublico
                })
            });
        }
        [HttpGet("HistorialCompleto")]
        public async Task<IActionResult> GetHistorialCompleto([FromQuery] int usuarioId, [FromQuery] string rol)
        {
            // 1. Iniciamos la consulta en Presupuestos e INCLUIMOS sus Cotizaciones
            var query = _context.Presupuestos
                .Include(p => p.Cotizaciones) // Esto trae la lista de cortinas de cada presupuesto
                .AsQueryable();

            // 2. Filtro de Seguridad para Tienda y Distribuidor
            // El Administrador se salta esto y ve todo.
            if (rol == "Tienda" || rol == "Distribuidor")
            {
                query = query.Where(p => p.UsuarioId == usuarioId);
            }

            // 3. Ejecutamos y ordenamos
            var resultados = await query
                .OrderByDescending(p => p.FechaCreacion)
                .ToListAsync();

            return Ok(resultados);
        }
    }
}