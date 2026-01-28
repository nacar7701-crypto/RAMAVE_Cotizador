using Microsoft.AspNetCore.Mvc;

namespace RAMAVE_Cotizador.Controllers
{
    public class ClientesController : Controller
{
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

    public IActionResult Historial()
    {
        var rol = HttpContext.Session.GetString("UsuarioRol");
        if (string.IsNullOrEmpty(rol)) return RedirectToAction("Login", "Auth");
        return View();
    }
}
}