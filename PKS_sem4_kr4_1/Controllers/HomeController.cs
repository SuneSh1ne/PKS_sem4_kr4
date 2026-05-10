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
            var cities = from c in _context.Cities select c;

            if (!string.IsNullOrEmpty(searchString))
            {
                cities = cities.Where(c => c.Name.Contains(searchString));
                ViewData["CurrentSearch"] = searchString;
            }

            return View(await cities.ToListAsync());
        }
    }
}