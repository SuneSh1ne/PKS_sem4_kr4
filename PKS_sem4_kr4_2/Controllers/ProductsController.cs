using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PKS_sem4_kr4_2.Data;
using PKS_sem4_kr4_2.Models;

namespace PKS_sem4_kr4_2.Controllers
{
    public class ProductsController : Controller
    {
        private readonly AppDbContext _context;

        public ProductsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Products
        public async Task<IActionResult> Index(string? category, string? search)
        {
            // Сначала получаем все продукты
            var products = await _context.Products
                .Include(p => p.ProductMaterials)
                .ThenInclude(pm => pm.Material)
                .ToListAsync();

            // Фильтр по категории (без учёта регистра)
            if (!string.IsNullOrEmpty(category))
            {
                products = products.Where(p => p.Category != null && 
                    p.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
                ViewBag.CurrentCategory = category;
            }

            // Поиск по названию (без учёта регистра)
            if (!string.IsNullOrEmpty(search))
            {
                products = products.Where(p => p.Name != null && 
                    p.Name.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
                ViewBag.CurrentSearch = search;
            }

            // Получаем список категорий для фильтра
            ViewBag.Categories = await _context.Products
                .Select(p => p.Category)
                .Distinct()
                .ToListAsync();

            return View(products);
        }

        // GET: Products/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.ProductMaterials)
                .ThenInclude(pm => pm.Material)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            return View(product);
        }

        // POST: Products/Create
        [HttpPost]
        public async Task<IActionResult> Create(Product product, int[] materialIds, decimal[] quantities)
        {
            if (ModelState.IsValid)
            {
                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                // Привязываем материалы
                if (materialIds != null && quantities != null)
                {
                    for (int i = 0; i < materialIds.Length; i++)
                    {
                        if (materialIds[i] > 0 && quantities[i] > 0)
                        {
                            var productMaterial = new ProductMaterial
                            {
                                ProductId = product.Id,
                                MaterialId = materialIds[i],
                                QuantityNeeded = quantities[i]
                            };
                            _context.ProductMaterials.Add(productMaterial);
                        }
                    }
                    await _context.SaveChangesAsync();
                }

                TempData["Success"] = "Продукт успешно создан";
                return RedirectToAction(nameof(Index));
            }
            TempData["Error"] = "Ошибка при создании продукта";
            return RedirectToAction(nameof(Index));
        }

        // POST: Products/Edit
        [HttpPost]
        public async Task<IActionResult> Edit(int id, Product product, int[] materialIds, decimal[] quantities)
        {
            if (id != product.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var existingProduct = await _context.Products.FindAsync(id);
                if (existingProduct == null) return NotFound();

                // Обновляем только нужные поля
                existingProduct.Name = product.Name;
                existingProduct.Description = product.Description;
                existingProduct.Category = product.Category;
                existingProduct.ProductionTimePerUnit = product.ProductionTimePerUnit;
                // MinimalStock остаётся прежним

                // Удаляем старые связи с материалами
                var existingMaterials = await _context.ProductMaterials
                    .Where(pm => pm.ProductId == id)
                    .ToListAsync();
                _context.ProductMaterials.RemoveRange(existingMaterials);

                // Добавляем новые связи
                if (materialIds != null && quantities != null)
                {
                    for (int i = 0; i < materialIds.Length; i++)
                    {
                        if (materialIds[i] > 0 && quantities[i] > 0)
                        {
                            var productMaterial = new ProductMaterial
                            {
                                ProductId = id,
                                MaterialId = materialIds[i],
                                QuantityNeeded = quantities[i]
                            };
                            _context.ProductMaterials.Add(productMaterial);
                        }
                    }
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = "Продукт обновлён";
                return RedirectToAction(nameof(Index));
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Products/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products
                .Include(p => p.ProductMaterials)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product != null)
            {
                // Удаляем связи с материалами
                _context.ProductMaterials.RemoveRange(product.ProductMaterials);
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Продукт удалён";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}