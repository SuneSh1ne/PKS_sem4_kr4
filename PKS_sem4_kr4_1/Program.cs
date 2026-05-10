using Microsoft.EntityFrameworkCore;
using TouristGuide.Models;  // Замените на ваше пространство имен

var builder = WebApplication.CreateBuilder(args);

// Добавление сервисов
builder.Services.AddControllersWithViews();

// Измените эту часть:
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=TouristGuide.db"));  // Создаст файл TouristGuide.db

var app = builder.Build();

// Создание базы данных
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.EnsureCreated();  // Создаст файл TouristGuide.db и таблицы
}

// Настройка middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();