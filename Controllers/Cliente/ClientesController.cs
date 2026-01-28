using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RAMAVE_Cotizador.Data; // Cambia esto por tu namespace real
using RAMAVE_Cotizador.Models;

namespace RAMAVE_Cotizador.Controllers
{
    public class ClientesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ClientesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var rol = HttpContext.Session.GetString("UsuarioRol");
            if (string.IsNullOrEmpty(rol)) return RedirectToAction("Login", "Auth");
            return View(); // Busca Views/Clientes/Index.cshtml
        }

        // AGREGA ESTO: Para que cargue tu archivo Home.cshtml
        public IActionResult Home()
        {
            var rol = HttpContext.Session.GetString("UsuarioRol");
            if (string.IsNullOrEmpty(rol)) return RedirectToAction("Login", "Auth");

            return View(); // Al llamarse la acción "Home", buscará "Home.cshtml" en la carpeta Clientes
        }

        public async Task<IActionResult> MisCotizaciones()
        {
            // 1. Validar sesión
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");
            var rol = HttpContext.Session.GetString("UsuarioRol");

            if (usuarioId == null || string.IsNullOrEmpty(rol))
            {
                return RedirectToAction("Login", "Auth");
            }

            // 2. Obtener historial filtrado por el ID del usuario logueado
            var historial = await _context.Cotizaciones
                .Where(c => c.UsuarioId == usuarioId) 
                .OrderByDescending(x => x.PresupuestoId)
                .ToListAsync();

            return View(historial);
        }
    }
}