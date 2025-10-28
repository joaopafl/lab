using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Pi_Odonto.Data;
using Pi_Odonto.Services;
using Pi_Odonto.Models;

var builder = WebApplication.CreateBuilder(args);

// === Serviços MVC ===
builder.Services.AddControllersWithViews();

// === Entity Framework ===
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 0, 21)));
});

// === Email ===
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddScoped<IEmailCadastroService, EmailCadastroService>();
builder.Services.AddScoped<EmailService>();

// === Autenticação com múltiplos cookies ===
builder.Services.AddAuthentication()
    .AddCookie("DentistaAuth", options =>
    {
        options.LoginPath = "/Auth/DentistaLogin";
        options.AccessDeniedPath = "/Auth/DentistaLogin";
        options.ExpireTimeSpan = TimeSpan.FromHours(2);
        options.SlidingExpiration = true;
        options.Cookie.Name = "DentistaAuth";
    })
    .AddCookie("AdminAuth", options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(2);
        options.SlidingExpiration = true;
        options.Cookie.Name = "AdminAuth";
    });

// === Políticas de autorização ===
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("DentistaOnly", policy =>
        policy.RequireClaim("TipoUsuario", "Dentista")
              .AddAuthenticationSchemes("DentistaAuth"));

    options.AddPolicy("AdminOnly", policy =>
        policy.RequireClaim("TipoUsuario", "Admin")
              .AddAuthenticationSchemes("AdminAuth"));

    options.AddPolicy("ResponsavelOnly", policy =>
        policy.RequireClaim("TipoUsuario", "Responsavel")
              .AddAuthenticationSchemes("AdminAuth")); // ou criar outro cookie se quiser
});

var app = builder.Build();

// === Pipeline ===
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// === Rotas exemplo Dentista ===
app.MapControllerRoute(
    name: "dentista",
    pattern: "Dentista/{action=Index}/{id?}",
    defaults: new { controller = "Dentista" });

// === Rotas Auth ===
app.MapControllerRoute(
    name: "dentista_login",
    pattern: "Auth/DentistaLogin",
    defaults: new { controller = "Auth", action = "DentistaLogin" });

app.MapControllerRoute(
    name: "admin_login",
    pattern: "Auth/Login",
    defaults: new { controller = "Auth", action = "Login" });

// === Rota padrão ===
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
