using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
            var products = _context.Products.AsQueryable();

            // Фильтр по категории
            if (!string.IsNullOrEmpty(category))
            {
                products = products.Where(p => p.Category == category);
                ViewBag.CurrentCategory = category;
            }

            // Поиск по названию
            if (!string.IsNullOrEmpty(search))
            {
                products = products.Where(p => p.Name.Contains(search));
                ViewBag.CurrentSearch = search;
            }

            // Получаем список категорий для фильтра
            ViewBag.Categories = await _context.Products
                .Select(p => p.Category)
                .Distinct()
                .ToListAsync();

            return View(await products.ToListAsync());
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
        public async Task<IActionResult> Create(Product product)
        {
            if (ModelState.IsValid)
            {
                _context.Add(product);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Продукт успешно создан";
                return RedirectToAction(nameof(Index));
            }
            TempData["Error"] = "Ошибка при создании продукта";
            return RedirectToAction(nameof(Index));
        }

        // POST: Products/Edit
        [HttpPost]
        public async Task<IActionResult> Edit(int id, Product product)
        {
            if (id != product.Id) return NotFound();

            if (ModelState.IsValid)
            {
                _context.Update(product);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Продукт обновлён";
                return RedirectToAction(nameof(Index));
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Products/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Продукт удалён";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}