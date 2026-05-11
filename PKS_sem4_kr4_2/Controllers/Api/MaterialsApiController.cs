using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PKS_sem4_kr4_2.Data;

namespace PKS_sem4_kr4_2.Controllers.Api
{
    [Route("api/materials")]
    [ApiController]
    public class MaterialsApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public MaterialsApiController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/materials/list
        [HttpGet("list")]
        public async Task<IActionResult> GetMaterialsList()
        {
            var materials = await _context.Materials
                .Select(m => new
                {
                    m.Id,
                    m.Name,
                    m.UnitOfMeasure,
                    m.Quantity,
                    m.MinimalStock
                })
                .ToListAsync();

            return Ok(materials);
        }
    }
}