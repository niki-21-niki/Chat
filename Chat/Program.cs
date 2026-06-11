
using Chat.Data;              
using Chat.Models;            
using Microsoft.AspNetCore.Identity;  
using Microsoft.EntityFrameworkCore;  
using Microsoft.EntityFrameworkCore.SqlServer;  

// Создание билдера приложения
var builder = WebApplication.CreateBuilder(args);

// Добавление сервисов в контейнер DI (Dependency Injection)

// Регистрация контекста базы данных с использованием SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Настройка системы идентификации (пользователи и роли)
builder.Services.AddIdentity<User, IdentityRole<int>>()
    .AddEntityFrameworkStores<AppDbContext>()  // Хранение данных в БД через AppDbContext
    .AddDefaultTokenProviders();  // Добавление провайдеров токенов по умолчанию

// Настройка SignalR (для работы в реальном времени)
builder.Services.AddSignalR(options => {
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();  // Подробные ошибки в разработке
    options.ClientTimeoutInterval = TimeSpan.FromMinutes(1);  // Таймаут клиента 1 минута
});

// Настройка политики CORS (разрешение всех запросов)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
        builder.AllowAnyOrigin()  // Разрешить любой источник
               .AllowAnyMethod()  // Разрешить любой HTTP-метод
               .AllowAnyHeader());  // Разрешить любые заголовки
});

// Добавление поддержки контроллеров и представлений
builder.Services.AddControllersWithViews();

// Сборка приложения
var app = builder.Build();

// Конфигурация middleware pipeline (цепочки обработки запросов)

app.UseHttpsRedirection();  // Перенаправление HTTP на HTTPS
app.UseStaticFiles();       // Поддержка статических файлов (wwwroot)
app.UseRouting();           // Маршрутизация запросов
app.UseAuthentication();    // Аутентификация пользователей
app.UseAuthorization();     // Авторизация доступа

// Настройка маршрутов для MVC
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Настройка конечной точки SignalR Hub
app.MapHub<ChatHub>("/chatHub", options =>
{
    options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets;  // Использовать WebSockets
});

// Применение миграций базы данных и заполнение начальными данными
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        await context.Database.MigrateAsync();  // Применение ожидающих миграций

        // Получение сервисов для работы с пользователями и ролями
        var userManager = services.GetRequiredService<UserManager<User>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<int>>>();

        // Инициализация начальных данных
        await SeedData.Initialize(services, userManager, roleManager);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Migration error");  // Логирование ошибок миграции
    }
}

// Применение политики CORS
app.UseCors("AllowAll");

// Запуск приложения
await app.RunAsync();