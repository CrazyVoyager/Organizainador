var builder = WebApplication.CreateBuilder(args);

// Agregar servicios antes de Build
builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews(); // ✅ Mover aquí

var app = builder.Build();

// Middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// Rutas
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
