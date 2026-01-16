using Microsoft.EntityFrameworkCore;
using RAMAVE_Cotizador.Models;

namespace RAMAVE_Cotizador.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<CostoMaterial> CostosMateriales { get; set; }
        public DbSet<Marca> Marcas { get; set; }
        public DbSet<Modelo> Modelos { get; set; }
        public DbSet<Tela> Telas { get; set; }
    }
}