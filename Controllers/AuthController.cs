using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RAMAVE_Cotizador.Data;
using RAMAVE_Cotizador.Models;

namespace RAMAVE_Cotizador.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuthController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.correo_electronico == model.Correo);

            if (usuario == null)
            {
                ModelState.AddModelError(nameof(model.Correo), "Correo incorrecto");
                return View(model);
            }

            if (!BCrypt.Net.BCrypt.Verify(model.Password, usuario.password))
            {
                ModelState.AddModelError(nameof(model.Password), "Contraseña incorrecta");
                return View(model);
            }

            // 🔥 CLAVE: GUARDAR ROL EN SESIÓN
            var rol = usuario.rol.Trim();
            HttpContext.Session.SetString("UsuarioRol", rol);

            Console.WriteLine($"ROL LOGUEADO = '{rol}'");

            // 🔥 CLAVE: NOMBRES CORRECTOS
            return rol switch
            {
                "Administrador" => RedirectToAction("Administrador", "Home"),
                "Tienda" => RedirectToAction("Tienda", "Home"),
                "Distribuidor" => RedirectToAction("Distribuidor", "Home"),
                _ => RedirectToAction("Login")
            };
        }
    }
}
