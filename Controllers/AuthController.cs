using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RAMAVE_Cotizador.Data;
using RAMAVE_Cotizador.Models;
using Microsoft.AspNetCore.Http; // Asegúrate de tener esta referencia

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
        public IActionResult Login() => View(new LoginViewModel());

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

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


            var rol = usuario.rol.Trim();

            // Guardar en sesión
            HttpContext.Session.SetString("UsuarioRol", rol);
            HttpContext.Session.SetInt32("UsuarioId", usuario.id);
            HttpContext.Session.SetString("UsuarioNombre", usuario.nombre ?? "Usuario");

            return rol switch
            {
                "Administrador" => RedirectToAction("Administrador", "Home"),
                "Tienda" or "Distribuidor" => RedirectToAction("Index", "Clientes"),
                "CapacitacionProduccion" => RedirectToAction("Produccion", "Capacitacion"),
                "CapacitacionVentas" or
                "CapacitacionInstalacion" => RedirectToAction("Index", "Home"),

                _ => RedirectToAction("Login")
            };
        }

        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Auth");
        }
    }
}