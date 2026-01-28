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
        public DbSet<Cotizaciones> Cotizaciones { get; set; }
        public DbSet<ConfigSoportes> ConfigSoportes { get; set; }
        public DbSet<Presupuesto> Presupuestos { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    // Esto le explica a EF exactamente cómo se llevan estas dos tablas
modelBuilder.Entity<Cotizaciones>()
    .HasOne(c => c.Presupuesto)
    .WithMany(p => p.Cotizaciones)
    .HasForeignKey(c => c.PresupuestoId); // Esto obliga a usar la columna que YA existe
}
    }
    
}