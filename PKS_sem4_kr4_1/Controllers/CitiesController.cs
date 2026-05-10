using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TouristGuide.Models;

namespace TouristGuide.Controllers
{
    public class CitiesController : Controller
    {
        private readonly AppDbContext _context;

        public CitiesController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var city = await _context.Cities
                .Include(c => c.Attractions)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (city == null) return NotFound();

            return View(city);
        }
    }
}