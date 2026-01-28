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

        // 1. LOGIN 
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest pedido)
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.correo_electronico == pedido.Correo);

            if (usuario == null || !BCrypt.Net.BCrypt.Verify(pedido.Password, usuario.password))
            {
                return Unauthorized(new { mensaje = "Correo o contraseña incorrectos" });
            }

            return Ok(new { mensaje = "¡Bienvenido!", usuario = usuario.nombre, rol = usuario.rol, id = usuario.id });
        }

        // 2. REGISTRAR 
        [HttpPost("registrar")]
        public async Task<IActionResult> Registrar([FromBody] Usuario nuevoUsuario)
        {
            try
            {
                nuevoUsuario.password = BCrypt.Net.BCrypt.HashPassword(nuevoUsuario.password);
                _context.Usuarios.Add(nuevoUsuario);
                await _context.SaveChangesAsync();
                return Ok(new { mensaje = "Usuario registrado con éxito" });
            }
            catch (Exception ex)
            {
                var mensajeReal = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return BadRequest(new { mensaje = "Error al registrar", detalle = mensajeReal });
            }
        }

        // 3. READ ALL (Traer todos)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Usuario>>> GetUsuarios([FromQuery] string? buscar)
        {
            var consulta = _context.Usuarios.AsQueryable();

            if (!string.IsNullOrEmpty(buscar))
            {
                // Busca coincidencias en nombre O en correo
                consulta = consulta.Where(u => u.nombre.Contains(buscar) 
                                            || u.correo_electronico.Contains(buscar));
            }

            return await consulta.ToListAsync();
        }

        // 4. Actualizar datos
        [HttpPut("{id}")]
        public async Task<IActionResult> Actualizar(int id, [FromBody] Usuario usuarioActualizado)
        {
            if (id != usuarioActualizado.id) return BadRequest(new { mensaje = "El ID no coincide" });

            var usuarioDb = await _context.Usuarios.FindAsync(id);
            if (usuarioDb == null) return NotFound(new { mensaje = "Usuario no encontrado" });

            // Actualizamos campos básicos
            usuarioDb.nombre = usuarioActualizado.nombre;
            usuarioDb.correo_electronico = usuarioActualizado.correo_electronico;
            usuarioDb.rol = usuarioActualizado.rol;

            // Solo encriptamos y cambiamos la clave si viene una nueva en el request
            if (!string.IsNullOrEmpty(usuarioActualizado.password) && usuarioActualizado.password != usuarioDb.password)
            {
                usuarioDb.password = BCrypt.Net.BCrypt.HashPassword(usuarioActualizado.password);
            }

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { mensaje = "Usuario actualizado correctamente" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = "Error al actualizar", detalle = ex.Message });
            }
        }

        // 5. Eliminar
        [HttpDelete("{id}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound(new { mensaje = "Usuario no encontrado" });

            _context.Usuarios.Remove(usuario);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Usuario eliminado correctamente" });
        }
    }
}