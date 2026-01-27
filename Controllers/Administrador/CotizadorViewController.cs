using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RAMAVE_Cotizador.Data;
using RAMAVE_Cotizador.Models;

namespace RAMAVE_Cotizador.Controllers
{
    [Route("Administrador/[controller]")]
    public class CotizacionesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CotizacionesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Administrador/Cotizaciones
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var lista = await _context.Cotizaciones
                .OrderByDescending(c => c.Id)
                .ToListAsync();

            return View("~/Views/Administrador/Cotizaciones/Index.cshtml", lista);
        }

        // --- NUEVOS MÉTODOS PARA EDICIÓN ---

        // GET: /Administrador/Cotizaciones/Editar/5
        [HttpGet("Editar/{id}")]
        public async Task<IActionResult> Editar(int id)
        {
            var cotizacion = await _context.Cotizaciones.FindAsync(id);
            if (cotizacion == null) return NotFound();

            // Retornamos la vista de edición cargando el modelo
            return View("~/Views/Administrador/Cotizaciones/Editar.cshtml", cotizacion);
        }

        // PUT: /Administrador/Cotizaciones/5
        [HttpPut("{id}")]
        public async Task<IActionResult> EditCotizacion(int id, [FromBody] Cotizaciones modelInput)
        {
            var model = await _context.Cotizaciones.FindAsync(id);

            if (model == null)
            {
                return NotFound(new { mensaje = "La cotización no existe." });
            }

            // 1. Actualizar campos editables
            model.Ancho = modelInput.Ancho;
            model.Alto = modelInput.Alto;
            model.IdTela = modelInput.IdTela;
            model.TipoCortina = modelInput.TipoCortina;
            model.Sistema = modelInput.Sistema;
            // Agrega aquí los demás campos que permitas editar

            // 2. RE-CALCULAR LOGICA DE INGENIERÍA
            // Aquí debes pegar el bloque de cálculos (M2, Costos, Precios) 
            // que usas en el método POST para que los totales se actualicen.

            model.M2 = model.Ancho * model.Alto;
            // ... (resto de tus cálculos) ...

            try
            {
                _context.Update(model);
                await _context.SaveChangesAsync();
                return Ok(new { mensaje = "Cotización actualizada y recalculada" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al guardar: " + ex.Message });
            }
        }

        // --- MÉTODOS EXISTENTES ---

        [HttpGet("GetCotizacion/{id}")]
        public async Task<ActionResult<Cotizaciones>> GetCotizacion(int id)
        {
            var cotizacion = await _context.Cotizaciones.FindAsync(id);
            if (cotizacion == null) return NotFound();
            return Ok(cotizacion);
        }

        [HttpDelete("Eliminar/{id}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var cot = await _context.Cotizaciones.FindAsync(id);
            if (cot == null) return NotFound();

            _context.Cotizaciones.Remove(cot);
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpGet("Crear")]
        public IActionResult Crear()
        {
            return View("~/Views/Administrador/Cotizaciones/Crear.cshtml");
        }

        [HttpGet("Detalle/{id}")]
        public async Task<IActionResult> Detalle(int id)
        {
            var cotizacion = await _context.Cotizaciones.FindAsync(id);
            if (cotizacion == null) return NotFound();

            return View("~/Views/Administrador/Cotizaciones/Detalle.cshtml", cotizacion);
        }

        [HttpGet("PresupuestoFinal/{id}")]
        public IActionResult PresupuestoFinal(int id)
        {
            ViewBag.PresupuestoId = id;
            return View("~/Views/Administrador/Cotizaciones/PresupuestoFinal.cshtml");
        }
    }
}