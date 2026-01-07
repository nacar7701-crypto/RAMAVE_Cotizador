using Microsoft.AspNetCore.Mvc;
using Cotizador.Web.Models;

namespace Cotizador.Web.Controllers
{
    public class AuthController : Controller
    {
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // 🔐 LOGIN DE EJEMPLO (después se conecta a BD)
            if (model.Email != "admin@empresa.com" || model.Password != "12345678")
            {
                ModelState.AddModelError(string.Empty, "Credenciales incorrectas");
                return View(model);
            }

            return RedirectToAction("Index", "Home");
        }
    }
}
