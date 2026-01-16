using Microsoft.AspNetCore.Mvc;

namespace RAMAVE_Cotizador.Controllers
{
    public class AdministradorController : Controller
    {
        // Vista principal: Listado de Telas
        public IActionResult Index() 
        {
            return View("~/Views/Administrador/Telas/Index.cshtml");
        }

        public IActionResult Telas()
        {
            return View("~/Views/Administrador/Telas/Index.cshtml");
        }

        // Vista: Gestión de Marcas y Modelos
        public IActionResult Crear()
        {
            return View("~/Views/Administrador/Telas/Crear.cshtml");
        }

        // Vista: Registro de Nueva Tela
        public IActionResult CrearTela()
        {
            return View("~/Views/Administrador/Telas/CrearTela.cshtml");
        }

        // --- AÑADE ESTA ACCIÓN PARA LA EDICIÓN ---
        // Se accede mediante: /Administrador/EditarTela/5
        public IActionResult EditarTela(int id)
        {
            // Pasamos el ID a la vista para que el JavaScript sepa qué registro consultar a la API
            ViewBag.IdTela = id; 
            return View("~/Views/Administrador/Telas/Editar.cshtml");
        }
    }
}