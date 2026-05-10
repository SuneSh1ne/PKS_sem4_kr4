using Microsoft.EntityFrameworkCore;
using PKS_sem4_kr4_2.Data;
using PKS_sem4_kr4_2.Models;

namespace PKS_sem4_kr4_2.Services
{
    public class ProductionService
    {
        private readonly AppDbContext _context;

        public ProductionService(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Рассчитывает время производства заказа
        /// Время = (Количество × ВремяНаЕдиницу) / КоэффициентЭффективности
        /// </summary>
        public async Task<int> CalculateProductionTime(int productId, int quantity, int? lineId = null)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
                throw new Exception("Продукт не найден");

            float efficiency = 1.0f;
            if (lineId.HasValue)
            {
                var line = await _context.ProductionLines.FindAsync(lineId.Value);
                if (line != null)
                    efficiency = line.EfficiencyFactor;
            }

            return (int)Math.Ceiling((quantity * product.ProductionTimePerUnit) / efficiency);
        }

        /// <summary>
        /// Рассчитывает дату завершения заказа
        /// </summary>
        public DateTime CalculateEndDate(DateTime startDate, int totalMinutes, int workingHoursPerDay = 8)
        {
            int minutesPerDay = workingHoursPerDay * 60;
            int totalDays = (int)Math.Ceiling((double)totalMinutes / minutesPerDay);
            
            // Учитываем только рабочие дни (упрощённо: исключаем выходные)
            DateTime endDate = startDate;
            int daysAdded = 0;
            while (daysAdded < totalDays)
            {
                endDate = endDate.AddDays(1);
                if (endDate.DayOfWeek != DayOfWeek.Saturday && endDate.DayOfWeek != DayOfWeek.Sunday)
                    daysAdded++;
            }
            
            return endDate;
        }

        /// <summary>
        /// Проверяет, достаточно ли материалов на складе для производства
        /// </summary>
        public async Task<Dictionary<string, decimal>> CheckMaterialsAvailability(int productId, int quantity)
        {
            var productMaterials = await _context.ProductMaterials
                .Include(pm => pm.Material)
                .Where(pm => pm.ProductId == productId)
                .ToListAsync();

            var shortages = new Dictionary<string, decimal>();

            foreach (var pm in productMaterials)
            {
                decimal needed = pm.QuantityNeeded * quantity;
                if (pm.Material.Quantity < needed)
                {
                    shortages.Add(pm.Material.Name, needed - pm.Material.Quantity);
                }
            }

            return shortages;
        }

        /// <summary>
        /// Списывает материалы со склада при запуске производства
        /// </summary>
        public async Task ConsumeMaterials(int productId, int quantity)
        {
            var productMaterials = await _context.ProductMaterials
                .Where(pm => pm.ProductId == productId)
                .ToListAsync();

            foreach (var pm in productMaterials)
            {
                var material = await _context.Materials.FindAsync(pm.MaterialId);
                if (material != null)
                {
                    material.Quantity -= pm.QuantityNeeded * quantity;
                }
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Получает список материалов с низким запасом
        /// </summary>
        public async Task<List<Material>> GetLowStockMaterials()
        {
            return await _context.Materials
                .Where(m => m.Quantity < m.MinimalStock)
                .ToListAsync();
        }

        /// <summary>
        /// Получает список доступных производственных линий
        /// </summary>
        public async Task<List<ProductionLine>> GetAvailableLines()
        {
            return await _context.ProductionLines
                .Where(l => l.Status == "Active" && l.CurrentWorkOrderId == null)
                .ToListAsync();
        }

        /// <summary>
        /// Запускает заказ в производство
        /// </summary>
        public async Task StartWorkOrder(int orderId, int lineId)
        {
            var order = await _context.WorkOrders.FindAsync(orderId);
            var line = await _context.ProductionLines.FindAsync(lineId);

            if (order == null || line == null)
                throw new Exception("Заказ или линия не найдены");

            if (line.CurrentWorkOrderId != null)
                throw new Exception("Линия уже занята");

            // Проверяем материалы
            var shortages = await CheckMaterialsAvailability(order.ProductId, order.Quantity);
            if (shortages.Any())
            {
                var shortageList = string.Join(", ", shortages.Select(s => $"{s.Key}: не хватает {s.Value}"));
                throw new Exception($"Недостаточно материалов: {shortageList}");
            }

            // Списываем материалы
            await ConsumeMaterials(order.ProductId, order.Quantity);

            // Назначаем заказ на линию
            order.ProductionLineId = lineId;
            order.Status = "InProgress";
            order.StartDate = DateTime.Now;
            
            // Рассчитываем время завершения
            int totalMinutes = await CalculateProductionTime(order.ProductId, order.Quantity, lineId);
            order.EstimatedEndDate = CalculateEndDate(DateTime.Now, totalMinutes);
            order.ProgressPercent = 0;

            // Обновляем линию
            line.CurrentWorkOrderId = orderId;

            await _context.SaveChangesAsync();
        }
    }
}