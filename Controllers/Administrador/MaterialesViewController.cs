using Microsoft.AspNetCore.Mvc;

namespace RAMAVE_Cotizador.Controllers
{
    public class MaterialesViewController : Controller
    {
        // Esta ruta será: localhost:XXXX/MaterialesView
        public IActionResult Index()
        {
            // Apuntamos a la ubicación física de tu archivo
            return View("~/Views/Administrador/Materiales/Mostrar.cshtml");
        }

        // Esta ruta será: localhost:XXXX/MaterialesView/Crear
        public IActionResult Crear()
        {
            return View("~/Views/Administrador/Materiales/Crear.cshtml");
        }

        // Esta ruta será: localhost:XXXX/MaterialesView/Editar/5
        public IActionResult Editar(int id)
        {
            ViewBag.MaterialId = id; // Pasamos el ID para que el JS sepa cuál editar
            return View("~/Views/Administrador/Materiales/Editar.cshtml");
        }
    }
}