var builder = WebApplication.CreateBuilder(args);

// 1️⃣ SERVICIOS
builder.Services.AddControllersWithViews();

// Swagger (solo desarrollo)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 2️⃣ PIPELINE
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

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
