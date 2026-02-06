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
            return View("~/Views/Capacitacion/Produccion/Cortinas/Cortinas.cshtml");
        }

        [HttpGet("Persianas")]
        public IActionResult Persianas()
        {
            if (!EsUsuarioAutorizado()) return RedirectToAction("Login", "Auth");
            return View("~/Views/Capacitacion/Produccion/Persianas/Persianas.cshtml");
        }

        // URL: /Capacitacion/CortinaDetalle/Ondulada
        [HttpGet("CortinaDetalle/{tipo}")]
        public IActionResult CortinaDetalle(string tipo)
        {
            if (!EsUsuarioAutorizado()) return RedirectToAction("Login", "Auth");

            // Pasamos el nombre del tipo a la vista para el título
            ViewBag.TipoCortina = tipo;

            return View("~/Views/Capacitacion/Produccion/Cortinas/CortinaDetalle.cshtml");
        }
        [HttpGet("Procesos")] // La URL será /Capacitacion/Procesos
        public IActionResult Procesos([FromQuery] string tipo) // Agregamos [FromQuery] para mayor claridad
        {
            if (!EsUsuarioAutorizado()) return RedirectToAction("Login", "Auth");

            if (string.IsNullOrEmpty(tipo)) return RedirectToAction("Persianas");

            // Usamos la ruta completa empezando con ~ para que no haya pierde
            return View("~/Views/Capacitacion/Produccion/Persianas/Procesos.cshtml");
        }
        [HttpGet("VerVideo")] // Esto crea la ruta /Capacitacion/VerVideo
        public IActionResult VerVideo(string tipo, string proceso)
        {
            if (!EsUsuarioAutorizado()) return RedirectToAction("Login", "Auth");

            // OJO: Verifica que la ruta de la vista sea exacta
            return View("~/Views/Capacitacion/Produccion/Persianas/VerVideo.cshtml");
        }
    }
}