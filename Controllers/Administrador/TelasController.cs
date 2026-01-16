using Microsoft.AspNetCore.Mvc;
using RAMAVE_Cotizador.Data;
using RAMAVE_Cotizador.Models;
using Microsoft.EntityFrameworkCore;

namespace RAMAVE_Cotizador.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TelasController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TelasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // SECCIÓN: OBTENER (GET)
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> GetTelas([FromQuery] string? filtro)
        {
            var consulta = _context.Telas
                .Include(t => t.modelo).ThenInclude(m => m.marca)
                .AsQueryable();

            if (!string.IsNullOrEmpty(filtro))
            {
                consulta = consulta.Where(t => t.modelo.marca.nombre.Contains(filtro) 
                                            || t.modelo.nombre.Contains(filtro) 
                                            || t.color_nombre.Contains(filtro)
                                            || t.catalogo.Contains(filtro));
            }

            var resultado = await consulta.Select(t => new {
                t.id,
                marca = t.modelo.marca.nombre,
                modelo = t.modelo.nombre,
                t.tipo,
                t.catalogo,
                t.color_nombre,
                t.ancho,
                t.precio_ml_corte,
                t.precio_ml_corte_iva,
                t.precio_ml_rollo,
                t.precio_ml_rollo_iva,
                t.costo_x_m2,
                cuidados = new { t.lavar, t.temp_agua, t.exprimir, t.planchar, t.blanqueador, t.jabon }
            }).ToListAsync();

            return Ok(resultado);
        }

        [HttpGet("marcas")]
        public async Task<ActionResult<IEnumerable<Marca>>> GetMarcas() => await _context.Marcas.ToListAsync();

        [HttpGet("modelos/{idMarca}")]
        public async Task<ActionResult<IEnumerable<Modelo>>> GetModelosPorMarca(int idMarca) 
            => await _context.Modelos.Where(m => m.id_marca == idMarca).ToListAsync();

        // ==========================================
        // SECCIÓN: CREAR (POST)
        // ==========================================

        [HttpPost("marcas")]
        public async Task<IActionResult> PostMarca([FromBody] Marca marca)
        {
            _context.Marcas.Add(marca);
            await _context.SaveChangesAsync();
            return Ok(new { mensaje = "Marca creada", id = marca.id });
        }

        [HttpPost("modelos")]
        public async Task<IActionResult> PostModelo([FromBody] Modelo modelo)
        {
            if (!await _context.Marcas.AnyAsync(m => m.id == modelo.id_marca))
                return BadRequest("La marca no existe.");
            _context.Modelos.Add(modelo);
            await _context.SaveChangesAsync();
            return Ok(new { mensaje = "Modelo creado", id = modelo.id });
        }

// --- CLASE PARA RECIBIR LOS DATOS DESDE SWAGGER/FRONT ---
        public class TelaCreateDto
        {
            public int id_modelo { get; set; }
            public string? catalogo { get; set; }
            public string? tipo { get; set; }
            public List<string>? ColoresNuevos { get; set; } // El array que mandaste
            public decimal ancho { get; set; }
            public decimal precio_ml_corte { get; set; }
            public decimal precio_ml_rollo { get; set; }
            // Puedes agregar aquí los campos de cuidados si quieres recibirlos también
        }

        [HttpPost]
        public async Task<IActionResult> PostTela([FromBody] TelaCreateDto dto)
        {
            try 
            {
                // 1. Creamos la entidad real que va a la base de datos
                var nuevaTela = new Tela
                {
                    id_modelo = dto.id_modelo,
                    catalogo = dto.catalogo,
                    tipo = dto.tipo,
                    ancho = dto.ancho,
                    precio_ml_corte = dto.precio_ml_corte,
                    precio_ml_rollo = dto.precio_ml_rollo,
                    // Aquí hacemos la magia: convertimos la lista a string
                    color_nombre = dto.ColoresNuevos != null ? string.Join(", ", dto.ColoresNuevos) : null
                };

                _context.Telas.Add(nuevaTela);
                await _context.SaveChangesAsync();
                
                return Ok(new { mensaje = "Tela guardada", id = nuevaTela.id, colores = nuevaTela.color_nombre });
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = "Error", detalle = ex.Message });
            }
        }


        // ==========================================
        // SECCIÓN: EDITAR (PUT)
        // ==========================================

        [HttpPut("marcas/{id}")]
        public async Task<IActionResult> PutMarca(int id, [FromBody] Marca marca)
        {
            var db = await _context.Marcas.FindAsync(id);
            if (db == null) return NotFound();
            db.nombre = marca.nombre;
            await _context.SaveChangesAsync();
            return Ok(new { mensaje = "Marca actualizada" });
        }

        [HttpPut("modelos/{id}")]
        public async Task<IActionResult> PutModelo(int id, [FromBody] Modelo modelo)
        {
            var db = await _context.Modelos.FindAsync(id);
            if (db == null) return NotFound();
            db.nombre = modelo.nombre;
            db.id_marca = modelo.id_marca;
            await _context.SaveChangesAsync();
            return Ok(new { mensaje = "Modelo actualizado" });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutTela(int id, [FromBody] Tela telaActualizada)
        {
            if (id != telaActualizada.id) return BadRequest(new { mensaje = "El ID no coincide" });

            // Validar que el modelo exista (para evitar el error de Foreign Key que te salió)
            var modeloExiste = await _context.Modelos.AnyAsync(m => m.id == telaActualizada.id_modelo);
            if (!modeloExiste) return BadRequest(new { mensaje = "El ID de modelo no existe." });

            var telaDb = await _context.Telas.FindAsync(id);
            if (telaDb == null) return NotFound(new { mensaje = "Tela no encontrada" });

            // --- LÓGICA DE COLORES UNIFICADA ---
            // Si el usuario editó el texto de colores (ej: quitó uno o agregó otro con comas)
            if (!string.IsNullOrEmpty(telaActualizada.color_nombre))
            {
                // Limpiamos el texto: quitamos espacios extra y evitamos entradas vacías
                var listaLimpia = telaActualizada.color_nombre
                    .Split(',')
                    .Select(c => c.Trim())
                    .Where(c => !string.IsNullOrEmpty(c));
                    
                telaDb.color_nombre = string.Join(", ", listaLimpia);
            }
            else 
            {
                telaDb.color_nombre = null;
            }

            // Actualizamos el resto de la información básica
            telaDb.id_modelo = telaActualizada.id_modelo;
            telaDb.catalogo = telaActualizada.catalogo;
            telaDb.tipo = telaActualizada.tipo;
            telaDb.ancho = telaActualizada.ancho;
            telaDb.existencia = telaActualizada.existencia;
            telaDb.precio_ml_corte = telaActualizada.precio_ml_corte;
            telaDb.precio_ml_rollo = telaActualizada.precio_ml_rollo;

            // Campos de cuidados
            telaDb.lavar = telaActualizada.lavar;
            telaDb.temp_agua = telaActualizada.temp_agua;
            telaDb.exprimir = telaActualizada.exprimir;
            telaDb.planchar = telaActualizada.planchar;
            telaDb.blanqueador = telaActualizada.blanqueador;
            telaDb.jabon = telaActualizada.jabon;

            await _context.SaveChangesAsync();
            return Ok(new { mensaje = "Registro y colores actualizados con éxito", colores = telaDb.color_nombre });
        }

        // ==========================================
        // SECCIÓN: ELIMINAR (DELETE)
        // ==========================================

        [HttpDelete("marcas/{id}")]
        public async Task<IActionResult> DeleteMarca(int id)
        {
            var marca = await _context.Marcas.Include(m => m.modelos).FirstOrDefaultAsync(m => m.id == id);
            if (marca == null) return NotFound();
            if (marca.modelos.Any()) return BadRequest("No puedes eliminar una marca con modelos activos.");
            _context.Marcas.Remove(marca);
            await _context.SaveChangesAsync();
            return Ok(new { mensaje = "Marca eliminada" });
        }

        [HttpDelete("modelos/{id}")]
        public async Task<IActionResult> DeleteModelo(int id)
        {
            var modelo = await _context.Modelos.FindAsync(id);
            if (modelo == null) return NotFound();
            _context.Modelos.Remove(modelo);
            await _context.SaveChangesAsync();
            return Ok(new { mensaje = "Modelo eliminado" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTela(int id)
        {
            var tela = await _context.Telas.FindAsync(id);
            if (tela == null) return NotFound();
            _context.Telas.Remove(tela);
            await _context.SaveChangesAsync();
            return Ok(new { mensaje = "Tela eliminada" });
        }
        // --- AGREGAR UN COLOR A UNA TELA EXISTENTE ---
        public class ColorRequest {
            public string color { get; set; } = string.Empty;
        }

    }
}