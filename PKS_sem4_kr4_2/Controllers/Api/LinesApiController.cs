using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PKS_sem4_kr4_2.Data;

namespace PKS_sem4_kr4_2.Controllers.Api
{
    [Route("api/lines")]
    [ApiController]
    public class LinesApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public LinesApiController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/lines/available
        [HttpGet("available")]
        public async Task<IActionResult> GetAvailableLines()
        {
            var lines = await _context.ProductionLines
                .Where(l => l.Status == "Active")
                .Select(l => new
                {
                    l.Id,
                    l.Name,
                    l.EfficiencyFactor,
                    HasWorkOrder = l.CurrentWorkOrderId != null
                })
                .ToListAsync();

            return Ok(lines);
        }
    }
}