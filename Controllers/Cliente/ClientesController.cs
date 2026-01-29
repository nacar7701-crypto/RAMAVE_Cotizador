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

            HttpContext.Session.SetString("UsuarioRol", rol);
            HttpContext.Session.SetInt32("UsuarioId", id);
            HttpContext.Session.SetString("UsuarioNombre", nombre);

            if (rol.Trim() == "Administrador")
                return RedirectToAction("Administrador", "Administrador");

            return RedirectToAction("Index");
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
        
        [HttpGet("PresupuestoFinal/{id}")]
        public IActionResult PresupuestoFinal(int id)
        {
            ViewBag.PresupuestoId = id;
            return View("~/Views/Clientes/PresupuestoFinal.cshtml");
        }
    }
}