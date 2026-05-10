using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PKS_sem4_kr4_2.Data;
using PKS_sem4_kr4_2.Models;

namespace PKS_sem4_kr4_2.Controllers
{
    public class MaterialsController : Controller
    {
        private readonly AppDbContext _context;

        public MaterialsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Materials
        public async Task<IActionResult> Index()
        {
            var materials = await _context.Materials
                .Include(m => m.ProductMaterials)
                .ThenInclude(pm => pm.Product)
                .ToListAsync();
            return View(materials);
        }

        // POST: Materials/Create
        [HttpPost]
        public async Task<IActionResult> Create(Material material)
        {
            if (ModelState.IsValid)
            {
                _context.Add(material);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Материал успешно добавлен";
                return RedirectToAction(nameof(Index));
            }
            TempData["Error"] = "Ошибка при добавлении материала";
            return RedirectToAction(nameof(Index));
        }

        // POST: Materials/UpdateStock
        [HttpPost]
        public async Task<IActionResult> UpdateStock(int id, decimal amount)
        {
            var material = await _context.Materials.FindAsync(id);
            if (material == null)
                return NotFound();

            material.Quantity += amount;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Запас материала '{material.Name}' пополнен на {amount} {material.UnitOfMeasure}";
            return RedirectToAction(nameof(Index));
        }

        // POST: Materials/Edit
        [HttpPost]
        public async Task<IActionResult> Edit(int id, Material material)
        {
            if (id != material.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(material);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Материал успешно обновлён";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MaterialExists(material.Id))
                        return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Materials/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var material = await _context.Materials.FindAsync(id);
            if (material != null)
            {
                _context.Materials.Remove(material);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Материал удалён";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool MaterialExists(int id)
        {
            return _context.Materials.Any(e => e.Id == id);
        }
    }
}