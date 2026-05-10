using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
            var orders = _context.WorkOrders
                .Include(w => w.Product)
                .Include(w => w.ProductionLine)
                .AsQueryable();

            // Фильтр по статусу
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

            // Фильтр по дате
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

                // Создаём заказ
                var order = new WorkOrder
                {
                    ProductId = productId,
                    Quantity = quantity,
                    ProductionLineId = productionLineId,
                    StartDate = DateTime.Now,
                    Status = "Pending",
                    ProgressPercent = 0
                };

                // Рассчитываем время производства
                int totalMinutes = await _productionService.CalculateProductionTime(productId, quantity, productionLineId);
                order.EstimatedEndDate = _productionService.CalculateEndDate(DateTime.Now, totalMinutes);

                _context.Add(order);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Заказ #{order.Id} создан. Расчётное время: {totalMinutes} мин.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Ошибка: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: WorkOrders/StartProduction
        [HttpPost]
        public async Task<IActionResult> StartProduction(int orderId, int lineId)
        {
            try
            {
                await _productionService.StartWorkOrder(orderId, lineId);
                TempData["Success"] = "Производство запущено!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
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

            // Если заказ выполнялся на линии, освобождаем линию
            if (order.ProductionLine != null && order.ProductionLine.CurrentWorkOrderId == orderId)
            {
                order.ProductionLine.CurrentWorkOrderId = null;
            }

            order.Status = "Cancelled";
            order.ProductionLineId = null;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Заказ #{orderId} отменён";
            return RedirectToAction(nameof(Index));
        }

        // POST: WorkOrders/UpdateProgress
        [HttpPost]
        public async Task<IActionResult> UpdateProgress(int orderId, int percent)
        {
            var order = await _context.WorkOrders
                .Include(w => w.ProductionLine)
                .FirstOrDefaultAsync(w => w.Id == orderId);

            if (order == null)
                return NotFound();

            order.ProgressPercent = percent;

            if (percent >= 100)
            {
                order.Status = "Completed";
                order.ActualEndDate = DateTime.Now;
                
                // Освобождаем линию
                if (order.ProductionLine != null)
                {
                    order.ProductionLine.CurrentWorkOrderId = null;
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Прогресс заказа #{orderId} обновлён до {percent}%";

            return RedirectToAction(nameof(Index));
        }
    }
}