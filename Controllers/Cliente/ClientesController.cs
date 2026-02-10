using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Necesario para Include y ToListAsync
using RAMAVE_Cotizador.Data; // Ajusta según tu namespace de Data
using RAMAVE_Cotizador.Models;

namespace RAMAVE_Cotizador.Controllers
{
    public class ClientesController : Controller
    {
        private readonly ApplicationDbContext _context; // El nombre de tu Contexto

        // Constructor para inyectar la base de datos
        public ClientesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult EstablecerSesion(string rol, int id, string nombre)
        {
            if (string.IsNullOrEmpty(rol)) return RedirectToAction("Login", "Auth");

            // Guardamos en sesión
            HttpContext.Session.SetString("UsuarioRol", rol.Trim());
            HttpContext.Session.SetInt32("UsuarioId", id);
            HttpContext.Session.SetString("UsuarioNombre", nombre);

            var rolLimpio = rol.Trim();

            // 🔹 Lógica de redirección corregida
            if (rolLimpio == "Administrador")
            {
                return RedirectToAction("Administrador", "Home"); // O "Administrador" dependiendo de tu Home
            }

            if (rolLimpio == "CapacitacionProduccion" ||
                rolLimpio == "CapacitacionVentas" ||
                rolLimpio == "CapacitacionInstalacion")
            {
                // Forzamos la ruta al nuevo controlador
                return RedirectToAction("Produccion", "Capacitacion");
            }

            // Por defecto para Tienda y Distribuidor
            return RedirectToAction("Index", "Clientes");
        }

        public IActionResult Index()
        {
            var rol = HttpContext.Session.GetString("UsuarioRol");
            if (string.IsNullOrEmpty(rol)) return RedirectToAction("Login", "Auth");
            return View();
        }

        public IActionResult Home()
        {
            var rol = HttpContext.Session.GetString("UsuarioRol");
            if (string.IsNullOrEmpty(rol)) return RedirectToAction("Login", "Auth");
            return View();
        }

        public async Task<IActionResult> Historial()
        {
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");
            if (usuarioId == null) return RedirectToAction("Login", "Auth");

            // Filtrado por UsuarioId para que solo vea lo suyo
            var historial = await _context.Presupuestos
                .Include(p => p.Cotizaciones)
                .Where(p => p.UsuarioId == usuarioId)
                .OrderByDescending(p => p.FechaCreacion)
                .ToListAsync();

            return View(historial);
        }

        [HttpGet]
        public IActionResult PresupuestoFinal(int id)
        {
            ViewBag.PresupuestoId = id;
            // Forzamos la ruta de la vista por si acaso
            return View("~/Views/Clientes/PresupuestoFinal.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");
            if (usuarioId == null) return RedirectToAction("Login", "Auth");

            // Buscamos la cotización específica
            var cotizacion = await _context.Cotizaciones
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cotizacion == null) return NotFound();

            // Aquí mandas a la vista donde tienes tu formulario de cotizador
            // usualmente pasas el modelo para precargar los datos
            return View(cotizacion); 
        }
    }
}