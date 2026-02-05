using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace RAMAVE_Cotizador.Controllers
{
    public class CapacitacionController : Controller
    {
        // Método para validar acceso (DRY - Don't Repeat Yourself)
        private bool EsUsuarioAutorizado()
        {
            var rol = HttpContext.Session.GetString("UsuarioRol");
            // Permitimos el acceso a los roles de capacitación y al administrador
            return rol == "CapacitacionProduccion" || 
                   rol == "CapacitacionVentas" || 
                   rol == "CapacitacionInstalacion" || 
                   rol == "Administrador";
        }

        // GET: /Capacitacion/Produccion
        public IActionResult Produccion()
        {
            if (!EsUsuarioAutorizado())
            {
                return RedirectToAction("Login", "Auth");
            }

            return View();
        }

        // GET: /Capacitacion/Cortinas
        // Esta es la vista que se abrirá al dar clic en la card de Cortinas
        public IActionResult Cortinas()
        {
            if (!EsUsuarioAutorizado()) return RedirectToAction("Login", "Auth");
            
            // Aquí retornarás la vista con los manuales o videos de cortinas
            return View(); 
        }

        // GET: /Capacitacion/Persianas
        // Esta es la vista que se abrirá al dar clic en la card de Persianas
        public IActionResult Persianas()
        {
            if (!EsUsuarioAutorizado()) return RedirectToAction("Login", "Auth");

            // Aquí retornarás la vista con los manuales o videos de persianas
            return View();
        }

        // GET: /Capacitacion/Ventas
        public IActionResult Ventas()
        {
            var rol = HttpContext.Session.GetString("UsuarioRol");
            if (rol != "CapacitacionVentas" && rol != "Administrador")
            {
                return RedirectToAction("Login", "Auth");
            }

            return View();
        }

        // GET: /Capacitacion/Instalacion
        public IActionResult Instalacion()
        {
            var rol = HttpContext.Session.GetString("UsuarioRol");
            if (rol != "CapacitacionInstalacion" && rol != "Administrador")
            {
                return RedirectToAction("Login", "Auth");
            }

            return View();
        }
    }
}