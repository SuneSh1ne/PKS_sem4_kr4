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
        /// Рассчитывает время производства заказа в минутах
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

            // Время = (Количество × ВремяНаЕдиницу) / Эффективность
            return (int)Math.Ceiling((quantity * product.ProductionTimePerUnit) / efficiency);
        }

        /// <summary>
        /// Рассчитывает дату завершения заказа (простой расчёт)
        /// </summary>
        public DateTime CalculateEndDate(DateTime startDate, int totalMinutes)
        {
            // Просто прибавляем минуты к текущему времени
            // Без учёта рабочих часов, чтобы было понятно
            return startDate.AddMinutes(totalMinutes);
        }

        /// <summary>
        /// Проверяет, достаточно ли материалов для производства
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
        /// Списывает материалы со склада
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
        /// Возвращает материалы на склад при отмене заказа.
        /// Возвращается количество пропорционально НЕВЫПОЛНЕННОМУ проценту.
        /// remainingPercent - процент, который остался до завершения (100 - progress)
        /// Округление ВНИЗ при дробном результате.
        /// </summary>
        public async Task ReturnMaterials(int productId, int quantity, int remainingPercent)
        {
            if (remainingPercent <= 0) return;

            var productMaterials = await _context.ProductMaterials
                .Where(pm => pm.ProductId == productId)
                .ToListAsync();

            decimal percentMultiplier = remainingPercent / 100m;

            foreach (var pm in productMaterials)
            {
                var material = await _context.Materials.FindAsync(pm.MaterialId);
                if (material != null)
                {
                    // Сколько материала нужно вернуть = всего_потрачено * процент_оставшийся
                    decimal totalUsed = pm.QuantityNeeded * quantity;
                    decimal amountToReturn = totalUsed * percentMultiplier;
                    
                    // Округляем ВНИЗ (Math.Floor)
                    amountToReturn = Math.Floor(amountToReturn);
                    
                    if (amountToReturn > 0)
                    {
                        material.Quantity += amountToReturn;
                    }
                }
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Получает список доступных линий
        /// </summary>
        public async Task<List<ProductionLine>> GetAvailableLines()
        {
            return await _context.ProductionLines
                .Where(l => l.Status == "Active" && l.CurrentWorkOrderId == null)
                .ToListAsync();
        }

        /// <summary>
        /// Рассчитывает текущий прогресс заказа на основе прошедшего времени
        /// </summary>
        public async Task<int> CalculateCurrentProgress(int orderId)
        {
            var order = await _context.WorkOrders
                .Include(w => w.Product)
                .Include(w => w.ProductionLine)
                .FirstOrDefaultAsync(w => w.Id == orderId);

            if (order == null || order.Status != "InProgress")
                return order?.ProgressPercent ?? 0;

            // Если заказ ещё не начался (будущая дата)
            if (order.StartDate > DateTime.Now)
                return 0;

            // Общее необходимое время в минутах
            int totalMinutes = await CalculateProductionTime(
                order.ProductId, 
                order.Quantity, 
                order.ProductionLineId);

            // Прошедшее время с начала в минутах
            int elapsedMinutes = (int)(DateTime.Now - order.StartDate).TotalMinutes;

            // Прогресс = (прошедшее / общее) * 100
            int progress = (int)Math.Min(100, Math.Round(((double)elapsedMinutes / totalMinutes) * 100));

            return progress;
        }

        /// <summary>
        /// Обновляет прогресс всех активных заказов
        /// </summary>
        public async Task UpdateAllProgresses()
        {
            var activeOrders = await _context.WorkOrders
                .Where(w => w.Status == "InProgress")
                .Include(w => w.ProductionLine)
                .ToListAsync();

            foreach (var order in activeOrders)
            {
                int progress = await CalculateCurrentProgress(order.Id);
                
                order.ProgressPercent = progress;

                if (progress >= 100)
                {
                    order.Status = "Completed";
                    order.ActualEndDate = DateTime.Now;
                    order.ProgressPercent = 100;
                    
                    // Освобождаем линию
                    if (order.ProductionLine != null && order.ProductionLine.CurrentWorkOrderId == order.Id)
                    {
                        order.ProductionLine.CurrentWorkOrderId = null;
                    }
                }
            }

            if (activeOrders.Any())
            {
                await _context.SaveChangesAsync();
            }
        }
    }
}