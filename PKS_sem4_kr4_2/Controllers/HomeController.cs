using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PKS_sem4_kr4_2.Data;

namespace PKS_sem4_kr4_2.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Статистика для дашборда
            ViewBag.ActiveOrders = await _context.WorkOrders.CountAsync(w => w.Status == "InProgress");
            ViewBag.PendingOrders = await _context.WorkOrders.CountAsync(w => w.Status == "Pending");
            ViewBag.CompletedOrders = await _context.WorkOrders.CountAsync(w => w.Status == "Completed");
            
            ViewBag.ActiveLines = await _context.ProductionLines.CountAsync(l => l.Status == "Active");
            ViewBag.TotalLines = await _context.ProductionLines.CountAsync();
            
            ViewBag.LowStockMaterials = await _context.Materials.CountAsync(m => m.Quantity < m.MinimalStock);
            ViewBag.TotalProducts = await _context.Products.CountAsync();

            // Последние заказы для таблицы
            ViewBag.RecentOrders = await _context.WorkOrders
                .Include(w => w.Product)
                .Include(w => w.ProductionLine)
                .OrderByDescending(w => w.Id)
                .Take(5)
                .ToListAsync();

            return View();
        }
    }
}