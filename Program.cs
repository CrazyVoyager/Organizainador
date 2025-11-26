using Microsoft.EntityFrameworkCore;
using Organizainador.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Organizainador.Services;

var builder = WebApplication.CreateBuilder(args);

// Agregar servicios al contenedor de dependencias
builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews();

// ==========================================================
// CONFIGURACIÓN DE AUTENTICACIÓN POR COOKIES
// ==========================================================

// 1. Configurar Autenticación por Cookies: Esto establece cómo se manejará la sesión
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        // Ruta a donde redirigir si se intenta acceder a una página restringida
        options.LoginPath = "/Login";

    });

// 2. Configurar PostgreSQL (mantenemos la configuración para el DbContext si lo usas en otras partes)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ==========================================================
// 🔑 CLAVE: CONFIGURACIÓN DE DAPPER
// ==========================================================

// 3. Registrar el UserService con factory method para inyectar conexión y logger
builder.Services.AddScoped<UserService>(provider =>
{
    var connectionString = provider.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection");
    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException("La cadena de conexión 'DefaultConnection' no fue encontrada en appsettings.");
    }
    var logger = provider.GetRequiredService<ILogger<UserService>>();
    return new UserService(connectionString, logger);
});

var app = builder.Build();

// Configurar el pipeline de HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Configurar archivos estáticos con UTF-8 para archivos JSON
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        if (ctx.File.Name.EndsWith(".json"))
        {
            ctx.Context.Response.Headers.Append("Content-Type", "application/json; charset=utf-8");
        }
    }
});

app.UseRouting();

// CRUCIAL: UseAuthentication debe ir antes de UseAuthorization
app.UseAuthentication();
app.UseAuthorization();

// Configurar endpoints
app.MapGet("/", context =>
{
    // Redirigir al inicio de sesión 
    context.Response.Redirect("/Login");
    return Task.CompletedTask;
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();