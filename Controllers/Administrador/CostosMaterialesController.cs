using Microsoft.AspNetCore.Mvc;
using RAMAVE_Cotizador.Data;
using RAMAVE_Cotizador.Models;
using Microsoft.EntityFrameworkCore;

namespace RAMAVE_Cotizador.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CostosMaterialesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CostosMaterialesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. OBTENER TODO (Traer la lista de precios)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CostoMaterial>>> GetMateriales([FromQuery] string? filtro)
        {
            var consulta = _context.CostosMateriales.AsQueryable();

            if (!string.IsNullOrEmpty(filtro))
            {
                // Busca en sistema, tipo o concepto
                consulta = consulta.Where(m => m.sistema.Contains(filtro) 
                                            || m.tipo.Contains(filtro) 
                                            || m.concepto.Contains(filtro));
            }

            return await consulta.ToListAsync();
        }

        // 2. OBTENER UNO SOLO (Por ID)
        [HttpGet("{id}")]
        public async Task<ActionResult<CostoMaterial>> GetMaterial(int id)
        {
            var material = await _context.CostosMateriales.FindAsync(id);
            if (material == null) return NotFound(new { mensaje = "Material no encontrado" });
            return material;
        }

        // 3. CREAR MATERIAL
        [HttpPost]
        public async Task<IActionResult> PostMaterial([FromBody] CostoMaterial material)
        {
            try 
            {
                _context.CostosMateriales.Add(material);
                await _context.SaveChangesAsync();
                return Ok(new { mensaje = "Material agregado correctamente", id = material.id });
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = "Error al guardar", detalle = ex.Message });
            }
        }

        // 4. ACTUALIZAR MATERIAL
        [HttpPut("{id}")]
        public async Task<IActionResult> PutMaterial(int id, [FromBody] CostoMaterial materialActualizado)
        {
            if (id != materialActualizado.id) return BadRequest(new { mensaje = "El ID no coincide" });

            var materialDb = await _context.CostosMateriales.FindAsync(id);
            if (materialDb == null) return NotFound(new { mensaje = "Material no encontrado" });

            // Actualizamos los campos
            materialDb.sistema = materialActualizado.sistema;
            materialDb.tipo = materialActualizado.tipo;
            materialDb.concepto = materialActualizado.concepto;
            materialDb.um = materialActualizado.um;
            materialDb.precio_unitario = materialActualizado.precio_unitario;

            await _context.SaveChangesAsync();
            return Ok(new { mensaje = "Material actualizado correctamente" });
        }

        // 5. ELIMINAR MATERIAL
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMaterial(int id)
        {
            var material = await _context.CostosMateriales.FindAsync(id);
            if (material == null) return NotFound(new { mensaje = "Material no encontrado" });

            _context.CostosMateriales.Remove(material);
            await _context.SaveChangesAsync();
            return Ok(new { mensaje = "Material eliminado correctamente" });
        }
    }
}