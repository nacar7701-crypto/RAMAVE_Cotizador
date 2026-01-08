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
            // Buscamos usando el nuevo nombre de la columna: correo_electronico
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.correo_electronico == pedido.Correo);

            if (usuario == null)
            {
                return Unauthorized(new { mensaje = "Correo o contraseña incorrectos" });
            }

            bool esValida = BCrypt.Net.BCrypt.Verify(pedido.Password, usuario.password);

            if (!esValida)
            {
                return Unauthorized(new { mensaje = "Correo o contraseña incorrectos" });
            }

            return Ok(new { 
                mensaje = "¡Bienvenido!", 
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