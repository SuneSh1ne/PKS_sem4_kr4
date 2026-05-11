using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PKS_sem4_kr4_2.Data;
using PKS_sem4_kr4_2.Models;
using PKS_sem4_kr4_2.Services;

namespace PKS_sem4_kr4_2.Controllers
{
    public class WorkOrdersController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ProductionService _productionService;

        public WorkOrdersController(AppDbContext context, ProductionService productionService)
        {
            _context = context;
            _productionService = productionService;
        }

        // GET: WorkOrders
        public async Task<IActionResult> Index(string? status, string? date)
        {
            await _productionService.UpdateAllProgresses();

            var orders = _context.WorkOrders
                .Include(w => w.Product)
                .Include(w => w.ProductionLine)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                if (status == "active")
                {
                    orders = orders.Where(w => w.Status == "InProgress");
                }
                else
                {
                    orders = orders.Where(w => w.Status == status);
                }
                ViewBag.CurrentStatus = status;
            }

            if (!string.IsNullOrEmpty(date))
            {
                if (date == "today")
                {
                    var today = DateTime.Today;
                    orders = orders.Where(w => w.StartDate.Date == today || w.EstimatedEndDate.Date == today);
                }
                else if (date == "overdue")
                {
                    orders = orders.Where(w => w.Status != "Completed" && w.EstimatedEndDate < DateTime.Now);
                }
                ViewBag.CurrentDate = date;
            }

            ViewBag.Statuses = new List<string> { "Pending", "InProgress", "Completed", "Cancelled" };

            return View(await orders.OrderByDescending(w => w.Id).ToListAsync());
        }

        // GET: WorkOrders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            await _productionService.UpdateAllProgresses();

            var order = await _context.WorkOrders
                .Include(w => w.Product)
                .ThenInclude(p => p.ProductMaterials)
                .ThenInclude(pm => pm.Material)
                .Include(w => w.ProductionLine)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (order == null) return NotFound();

            return View(order);
        }

        // POST: WorkOrders/Create
        [HttpPost]
        public async Task<IActionResult> Create(int productId, int quantity, int? productionLineId)
        {
            try
            {
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                {
                    TempData["Error"] = "Продукт не найден";
                    return RedirectToAction(nameof(Index));
                }

                // Просто создаём заказ со статусом Pending
                // НЕ списываем материалы, НЕ рассчитываем время
                var order = new WorkOrder
                {
                    ProductId = productId,
                    Quantity = quantity,
                    ProductionLineId = productionLineId,
                    StartDate = DateTime.MinValue,  // Будет пересчитано при запуске
                    EstimatedEndDate = DateTime.MinValue,  // Будет пересчитано при запуске
                    Status = "Pending",
                    ProgressPercent = 0
                };

                _context.Add(order);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Заказ #{order.Id} создан. Ожидает запуска.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Ошибка: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: WorkOrders/StartOrder
        [HttpPost]
        public async Task<IActionResult> StartOrder(int orderId)
        {
            try
            {
                var order = await _context.WorkOrders
                    .Include(w => w.Product)
                    .Include(w => w.ProductionLine)
                    .FirstOrDefaultAsync(w => w.Id == orderId);

                if (order == null)
                {
                    TempData["Error"] = "Заказ не найден";
                    return RedirectToAction(nameof(Index));
                }

                if (order.Status != "Pending")
                {
                    TempData["Error"] = "Можно запустить только ожидающий заказ";
                    return RedirectToAction(nameof(Index));
                }

                // Проверяем, что линия назначена и доступна
                if (order.ProductionLineId == null)
                {
                    TempData["Error"] = "Не назначена производственная линия";
                    return RedirectToAction(nameof(Index));
                }

                var line = await _context.ProductionLines.FindAsync(order.ProductionLineId);
                if (line == null)
                {
                    TempData["Error"] = "Линия не найдена";
                    return RedirectToAction(nameof(Index));
                }

                if (line.Status != "Active")
                {
                    TempData["Error"] = "Линия не активна";
                    return RedirectToAction(nameof(Index));
                }

                if (line.CurrentWorkOrderId != null)
                {
                    TempData["Error"] = "Линия уже занята другим заказом";
                    return RedirectToAction(nameof(Index));
                }

                // Проверяем материалы
                var shortages = await _productionService.CheckMaterialsAvailability(order.ProductId, order.Quantity);
                if (shortages.Any())
                {
                    var shortageList = string.Join(", ", shortages.Select(s => $"{s.Key}: не хватает {s.Value}"));
                    TempData["Error"] = $"Недостаточно материалов: {shortageList}";
                    return RedirectToAction(nameof(Index));
                }

                // Только теперь списываем материалы
                await _productionService.ConsumeMaterials(order.ProductId, order.Quantity);

                // Рассчитываем время
                int totalMinutes = await _productionService.CalculateProductionTime(
                    order.ProductId, 
                    order.Quantity, 
                    order.ProductionLineId);

                // Пересчитываем даты
                order.StartDate = DateTime.Now;
                order.EstimatedEndDate = _productionService.CalculateEndDate(DateTime.Now, totalMinutes);
                order.Status = "InProgress";
                order.ProgressPercent = 0;

                // Назначаем заказ на линию
                line.CurrentWorkOrderId = order.Id;

                await _context.SaveChangesAsync();

                TempData["Success"] = $"Заказ #{order.Id} запущен! Расчётное время: {totalMinutes} мин.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Ошибка: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: WorkOrders/CancelOrder
        [HttpPost]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            var order = await _context.WorkOrders
                .Include(w => w.ProductionLine)
                .FirstOrDefaultAsync(w => w.Id == orderId);

            if (order == null)
            {
                TempData["Error"] = "Заказ не найден";
                return RedirectToAction(nameof(Index));
            }

            if (order.Status == "Completed")
            {
                TempData["Error"] = "Нельзя отменить завершённый заказ";
                return RedirectToAction(nameof(Index));
            }

            // Если заказ в процессе — останавливаем и возвращаем материалы
            if (order.Status == "InProgress")
            {
                await _productionService.UpdateAllProgresses();
                
                int currentProgress = order.ProgressPercent;
                int remainingPercent = 100 - currentProgress;

                if (remainingPercent > 0)
                {
                    await _productionService.ReturnMaterials(order.ProductId, order.Quantity, remainingPercent);
                }

                // Освобождаем линию
                if (order.ProductionLine != null && order.ProductionLine.CurrentWorkOrderId == orderId)
                {
                    order.ProductionLine.CurrentWorkOrderId = null;
                }
            }
            
            // Если заказ ожидает (Pending) — просто отменяем, материалы не трогаем
            // (они ещё не были списаны)

            order.Status = "Cancelled";
            order.ProductionLineId = null;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Заказ #{orderId} отменён.";
            return RedirectToAction(nameof(Index));
        }
    }
}