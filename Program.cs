using RAMAVE_Cotizador.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. CONFIGURACIÓN DE LA BASE DE DATOS (DOCKER)
// Solo declaramos la variable UNA vez
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Registramos el contexto para que el sistema pueda usar la base de datos
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// 2. AGREGAR SERVICIOS
builder.Services.AddControllersWithViews(); // Para tus páginas HTML

// 1️⃣ SERVICIOS
builder.Services.AddControllersWithViews();

// Swagger (solo desarrollo)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(); // Para probar la API

builder.Services.AddCors(options =>
{
    options.AddPolicy("PermitirTodo", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

// 2️⃣ PIPELINE
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(); // Esto habilita la página de pruebas
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// app.UseHttpsRedirection(); 
app.UseStaticFiles();
app.UseRouting();
app.UseCors("PermitirTodo");
app.UseAuthorization();

// Importante: Esto permite que los controladores de tipo API funcionen
app.MapControllers(); 

// Ruta para tus vistas normales (Home/Index)
// HTTPS comentado para Docker / LAN
// app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseRouting();

app.UseAuthorization();

// 3️⃣ RUTA INICIAL → LOGIN
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.Run();