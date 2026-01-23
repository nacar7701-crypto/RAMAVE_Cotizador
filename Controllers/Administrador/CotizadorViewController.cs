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

            // ESPECIFICAMOS LA RUTA MANUALMENTE para evitar el error de "View not found"
            return View("~/Views/Administrador/Cotizaciones/Index.cshtml", lista);
        }

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

        // GET: /Administrador/Cotizaciones/Nuevo
        [HttpGet("Nuevo")]
        public IActionResult Nuevo()
        {
            // También especificamos la ruta para la vista de creación
            return View("~/Views/Administrador/Cotizaciones/Nuevo.cshtml");
        }

        [HttpGet("Detalle/{id}")]
        public async Task<IActionResult> Detalle(int id)
        {
            var cotizacion = await _context.Cotizaciones.FindAsync(id);
            if (cotizacion == null) return NotFound();

            return View("~/Views/Administrador/Cotizaciones/Detalle.cshtml", cotizacion);
        }
    }
}