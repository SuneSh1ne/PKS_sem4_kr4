using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PKS_sem4_kr4_2.Data;

namespace PKS_sem4_kr4_2.Controllers.Api
{
    [Route("api/products")]
    [ApiController]
    public class ProductsApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductsApiController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/products/list
        [HttpGet("list")]
        public async Task<IActionResult> GetProductsList()
        {
            var products = await _context.Products
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.ProductionTimePerUnit,
                    p.Category
                })
                .ToListAsync();

            return Ok(products);
        }
    }
}