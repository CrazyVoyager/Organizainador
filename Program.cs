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

// 3. Registrar la cadena de conexión como un string para inyección en servicios.
// El UserService lo recibirá para crear su NpgsqlConnection con Dapper.
builder.Services.AddSingleton<string>(provider =>
{
    // Obtiene la cadena de conexión configurada
    var connectionString = provider.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection");
    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException("La cadena de conexión 'DefaultConnection' no fue encontrada en appsettings.");
    }
    return connectionString;
});

// 4. Registrar el UserService, que ahora recibirá el string de la cadena de conexión
builder.Services.AddScoped<UserService>();

var app = builder.Build();

// Configurar el pipeline de HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
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

// Configurar archivos estáticos con UTF-8
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

app.Run();