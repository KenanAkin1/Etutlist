using Etutlist.Data;
using Etutlist.Models;
using Etutlist.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Connection string'i appsettings.json'dan al
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Servisleri ekle
builder.Services.AddScoped<EtutPlanlamaService>();
builder.Services.AddScoped<TelafiDersService>();

// MVC
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Hata yönetimi ve güvenlik
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// Default route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();
    context.Database.Migrate(); // Migration uygula
    //SeedData.Initialize(context); // Seed verileri ekle
}

app.Run();
