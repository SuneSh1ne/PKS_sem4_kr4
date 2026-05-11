using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PKS_sem4_kr4_2.Data;
using PKS_sem4_kr4_2.Services;
using System.Globalization;


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
            var line = await _context.ProductionLines
                .Include(l => l.CurrentWorkOrder)
                .FirstOrDefaultAsync(l => l.Id == lineId);
                
            if (line == null) return NotFound();

            // Если останавливаем линию и на ней есть активный заказ
            if (status == "Stopped" && line.CurrentWorkOrderId != null && line.CurrentWorkOrder != null)
            {
                var order = line.CurrentWorkOrder;
                
                // Обновляем прогресс перед остановкой
                var productionService = HttpContext.RequestServices.GetRequiredService<PKS_sem4_kr4_2.Services.ProductionService>();
                await productionService.UpdateAllProgresses();
                
                // Получаем актуальный прогресс
                int currentProgress = order.ProgressPercent;
                int remainingPercent = 100 - currentProgress;
                
                // Возвращаем материалы за невыполненную часть
                if (remainingPercent > 0)
                {
                    await productionService.ReturnMaterials(order.ProductId, order.Quantity, remainingPercent);
                }
                
                // Останавливаем заказ
                order.Status = "Cancelled";
                order.ProductionLineId = null;
            }

            // Меняем статус линии
            line.Status = status;
            line.CurrentWorkOrderId = null;

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Статус линии '{line.Name}' изменён на '{status}'";

            return RedirectToAction(nameof(Index));
        }

        // POST: ProductionLines/UpdateEfficiency
        [HttpPost]
        public async Task<IActionResult> UpdateEfficiency(int lineId, string efficiencyFactorStr)
        {
            var line = await _context.ProductionLines.FindAsync(lineId);
            if (line == null) return NotFound();

            // Парсим строку в число (заменяем запятую на точку для надёжности)
            string normalized = efficiencyFactorStr.Replace(',', '.');
            
            if (!float.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out float efficiencyFactor))
            {
                TempData["Error"] = "Некорректное значение коэффициента";
                return RedirectToAction(nameof(Index));
            }

            // Ограничиваем: минимум 0.1, максимум 2.0
            efficiencyFactor = Math.Clamp(efficiencyFactor, 0.1f, 2.0f);
            
            // Округляем до одного знака
            efficiencyFactor = (float)Math.Round(efficiencyFactor, 1);
            
            float oldValue = line.EfficiencyFactor;
            line.EfficiencyFactor = efficiencyFactor;

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Коэффициент линии '{line.Name}' изменён: {oldValue:F1}x → {efficiencyFactor:F1}x";

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