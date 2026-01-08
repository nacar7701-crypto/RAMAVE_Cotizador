using Microsoft.AspNetCore.Mvc;
using RAMAVE_Cotizador.Data;
using RAMAVE_Cotizador.Models;
using Microsoft.EntityFrameworkCore;

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

            string email = model.Correo.Trim();
            string password = model.Password;

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.correo_electronico == email);

            if (usuario == null)
            {
                ViewBag.Error = "Usuario o contraseña incorrectos";
                return View(model);
            }

            // ✅ COMPARACIÓN CORRECTA CON BCRYPT
            bool passwordValido = BCrypt.Net.BCrypt.Verify(password, usuario.password);

            if (!passwordValido)
            {
                ViewBag.Error = "Usuario o contraseña incorrectos";
                return View(model);
            }

            // 🔐 LOGIN EXITOSO
            // (más adelante aquí va sesión / cookies)
            return RedirectToAction("Index", "Home");
        }
    }
}
