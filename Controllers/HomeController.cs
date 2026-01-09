using Microsoft.AspNetCore.Mvc;

namespace RAMAVE_Cotizador.Controllers
{
    public class HomeController : Controller
    {
        // Obtenemos el rol desde sesión
        private string? Rol => HttpContext.Session.GetString("UsuarioRol");

        // Redirección si no tiene acceso
        private IActionResult SinAcceso()
        {
            return RedirectToAction("Login", "Auth");
        }

        // 🔴 HOME ADMINISTRADOR
        public IActionResult Administrador()
        {
            if (Rol != "Administrador")
                return SinAcceso();

            return View("/Views/Administrador/Administrador.cshtml");
        }

        // 🔵 HOME TIENDA
        public IActionResult Tienda()
        {
            if (Rol != "Tienda")
                return SinAcceso();

            return View("/Views/Distribuidor_Tienda/Home.cshtml");
        }

        // 🟢 HOME DISTRIBUIDOR
        public IActionResult Distribuidor()
        {
            if (Rol != "Distribuidor")
                return SinAcceso();

            return View("/Views/Distribuidor_Tienda/Home.cshtml");
        }
    }
}
