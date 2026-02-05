using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace RAMAVE_Cotizador.Controllers // <- QUITAR el ".Capacitacion" si lo tiene
{
    [Route("Capacitacion")] // <- Esto define que la URL empieza con /Capacitacion
    public class CapacitacionController : Controller
    {
        private bool EsUsuarioAutorizado()
        {
            var rol = HttpContext.Session.GetString("UsuarioRol")?.Trim();
            return rol == "CapacitacionProduccion" || 
                   rol == "CapacitacionVentas" || 
                   rol == "CapacitacionInstalacion" || 
                   rol == "Administrador";
        }

        [HttpGet("Produccion")] // <- Esto define que la URL es /Capacitacion/Produccion
        public IActionResult Produccion()
        {
            if (!EsUsuarioAutorizado()) return RedirectToAction("Login", "Auth");

            // AL ESTAR EN SUBCARPETA, DEBES DAR LA RUTA COMPLETA DE LA VISTA
            return View("~/Views/Capacitacion/Produccion/Produccion.cshtml");
        }

        [HttpGet("Cortinas")]
        public IActionResult Cortinas()
        {
            if (!EsUsuarioAutorizado()) return RedirectToAction("Login", "Auth");
            return View("~/Views/Capacitacion/Produccion/Cortinas.cshtml");
        }

        [HttpGet("Persianas")]
        public IActionResult Persianas()
        {
            if (!EsUsuarioAutorizado()) return RedirectToAction("Login", "Auth");
            return View("~/Views/Capacitacion/Produccion/Persianas.cshtml");
        }
    }
}