using Microsoft.AspNetCore.Mvc;
using RAMAVE_Cotizador.Data;
using RAMAVE_Cotizador.Models;
using Microsoft.EntityFrameworkCore;

namespace RAMAVE_Cotizador.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsuariosController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UsuariosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // --- ENDPOINT DE LOGIN ---
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest pedido)
        {
            // 1. Buscar al usuario por correo
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.correo == pedido.Correo);

            // 2. Si no existe, error de seguridad genérico
            if (usuario == null)
            {
                return Unauthorized(new { mensaje = "Correo o contraseña incorrectos" });
            }

            // 3. Verificar si la contraseña coincide usando BCrypt
            bool esValida = BCrypt.Net.BCrypt.Verify(pedido.Password, usuario.password);

            if (!esValida)
            {
                return Unauthorized(new { mensaje = "Correo o contraseña incorrectos" });
            }

            // 4. Éxito: Devolvemos datos básicos
            return Ok(new
            {
                mensaje = "¡Bienvenido al sistema!",
                usuario = usuario.nombre,
                rol = usuario.rol
            });
        }

        // --- ENDPOINT DE REGISTRO ---
        [HttpPost("registrar")]
        public async Task<IActionResult> Registrar([FromBody] Usuario nuevoUsuario)
        {
            try
            {
                // Encriptar la clave antes de guardarla en la DB
                nuevoUsuario.password = BCrypt.Net.BCrypt.HashPassword(nuevoUsuario.password);

                _context.Usuarios.Add(nuevoUsuario);
                await _context.SaveChangesAsync();
                
                return Ok(new { 
                    mensaje = "Usuario registrado con éxito", 
                    usuario = nuevoUsuario.nombre 
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { 
                    mensaje = "Error al registrar", 
                    detalle = ex.Message 
                });
            }
        }
    }

    // Clase auxiliar para recibir los datos del Login
    public class LoginRequest
    {
        public string Correo { get; set; } = null!;
        public string Password { get; set; } = null!;
    }
}