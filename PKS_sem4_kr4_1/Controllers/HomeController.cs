using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TouristGuide.Models;

namespace TouristGuide.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string searchString)
        {
            // Получаем все города
            var cities = await _context.Cities.ToListAsync();

            // Поиск без учёта регистра
            if (!string.IsNullOrEmpty(searchString))
            {
                cities = cities.Where(c => c.Name != null && 
                    c.Name.Contains(searchString, StringComparison.OrdinalIgnoreCase)).ToList();
                ViewData["CurrentSearch"] = searchString;
            }

            return View(cities);
        }
    }
}