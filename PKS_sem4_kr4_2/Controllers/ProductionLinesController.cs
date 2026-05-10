using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PKS_sem4_kr4_2.Data;
using PKS_sem4_kr4_2.Models;

namespace PKS_sem4_kr4_2.Controllers
{
    public class ProductionLinesController : Controller
    {
        private readonly AppDbContext _context;

        public ProductionLinesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: ProductionLines
        public async Task<IActionResult> Index()
        {
            var lines = await _context.ProductionLines
                .Include(l => l.CurrentWorkOrder)
                .ThenInclude(w => w.Product)
                .Include(l => l.WorkOrders)
                .ThenInclude(w => w.Product)
                .ToListAsync();

            return View(lines);
        }

        // POST: ProductionLines/UpdateStatus
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int lineId, string status)
        {
            var line = await _context.ProductionLines.FindAsync(lineId);
            if (line == null) return NotFound();

            line.Status = status;
            
            if (status == "Stopped")
            {
                line.CurrentWorkOrderId = null;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Статус линии '{line.Name}' изменён на '{status}'";

            return RedirectToAction(nameof(Index));
        }

        // POST: ProductionLines/UpdateEfficiency
        [HttpPost]
        public async Task<IActionResult> UpdateEfficiency(int lineId, float efficiencyFactor)
        {
            var line = await _context.ProductionLines.FindAsync(lineId);
            if (line == null) return NotFound();

            // Ограничиваем коэффициент
            efficiencyFactor = Math.Clamp(efficiencyFactor, 0.5f, 2.0f);
            line.EfficiencyFactor = efficiencyFactor;

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Коэффициент линии '{line.Name}' изменён на {efficiencyFactor:F1}";

            return RedirectToAction(nameof(Index));
        }

        // GET: ProductionLines/Schedule/5
        public async Task<IActionResult> Schedule(int? id)
        {
            if (id == null) return NotFound();

            var line = await _context.ProductionLines
                .Include(l => l.WorkOrders)
                .ThenInclude(w => w.Product)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (line == null) return NotFound();

            return View(line);
        }
    }
}