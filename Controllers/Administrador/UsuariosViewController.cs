using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RAMAVE_Cotizador.Data;
using RAMAVE_Cotizador.Models;

namespace RAMAVE_Cotizador.Controllers
{
    public class UsuariosViewController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UsuariosViewController(ApplicationDbContext context)
        {
            _context = context;
        }

        // LISTADO
        public async Task<IActionResult> Index()
        {

            return View("~/Views/Administrador/Usuarios/Mostrar.cshtml");
        }

        // CREATE (GET)
        [HttpGet]
        public IActionResult Create()
        {
            return View("~/Views/Administrador/Usuarios/Crear.cshtml");
        }

        // CREATE (POST)
        [HttpPost]
        public async Task<IActionResult> Create(UsuarioCreateViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var usuario = new Usuario
            {
                nombre = model.Nombre,
                correo_electronico = model.Correo,
                rol = model.Rol,
                password = BCrypt.Net.BCrypt.HashPassword(model.Password)
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // EDIT (GET)
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();

            ViewBag.Id = id;
            return View("~/Views/Administrador/Usuarios/Editar.cshtml");
        }

        // DELETE (GET) - Opcional: Para mostrar una pantalla de "u00bfEstu00e1 seguro?"
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();

            return View(usuario);
        }

        // DELETE (POST) - La acciu00f3n real de eliminar
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken] // Seguridad contra ataques CSRF
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario != null)
            {
                _context.Usuarios.Remove(usuario);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}