using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. CONFIGURACIÓN DE LA BASE DE DATOS (DOCKER)
// Busca la conexión que guardamos en user-secrets
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
// builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString)); 

// 2. AGREGAR SERVICIOS
builder.Services.AddControllersWithViews();

// Agregamos Swagger para poder probar la API
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 3. CONFIGURAR EL PIPELINE (CÓMO RESPONDE EL SERVIDOR)
if (app.Environment.IsDevelopment())
{
    // ACTIVAMOS SWAGGER AQUÍ
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// app.UseHttpsRedirection(); // Comentado para evitar el aviso de "warn" en local
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// Ruta por defecto para tus vistas (HTML)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
