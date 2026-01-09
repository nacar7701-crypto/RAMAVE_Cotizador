using Microsoft.AspNetCore.Mvc;

namespace RAMAVE_Cotizador.Controllers
{
    public class UsuariosViewController : Controller
    {
        private bool EsAdmin =>
            HttpContext.Session.GetString("UsuarioRol") == "Administrador";

        private IActionResult SinAcceso() =>
            RedirectToAction("Login", "Auth");

        public IActionResult Index()
        {
            if (!EsAdmin) return SinAcceso();
            return View();
        }

        public IActionResult Create()
        {
            if (!EsAdmin) return SinAcceso();
            return View();
        }

        public IActionResult Edit(int id)
        {
            if (!EsAdmin) return SinAcceso();
            ViewBag.Id = id;
            return View();
        }
    }
}
