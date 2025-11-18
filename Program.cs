using Etutlist.Data;
using Etutlist.Models;
using Etutlist.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ✅ Kestrel ayarları - HTTP 431 hatasını önle
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestHeadersTotalSize = 32768; // 32 KB (default: 32 KB)
    options.Limits.MaxRequestLineSize = 8192; // 8 KB (default: 8 KB)
});

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

// Migration otomatik uygula (opsiyonel - production'da dikkatli kullan)
try
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<AppDbContext>();
        var logger = services.GetRequiredService<ILogger<Program>>();
        
        try
        {
            context.Database.Migrate();
            logger.LogInformation("Database migrations applied successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while migrating the database.");
            // Migration hatası uygulamayı durdurmasın
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Migration Error: {ex.Message}");
}

app.Run();
