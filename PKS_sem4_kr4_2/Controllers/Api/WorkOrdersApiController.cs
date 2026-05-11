using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PKS_sem4_kr4_2.Data;
using PKS_sem4_kr4_2.Services;

namespace PKS_sem4_kr4_2.Controllers.Api
{
    [Route("api/workorders")]
    [ApiController]
    public class WorkOrdersApiController : ControllerBase
    {
        private readonly ProductionService _productionService;
        private readonly AppDbContext _context;

        public WorkOrdersApiController(ProductionService productionService, AppDbContext context)
        {
            _productionService = productionService;
            _context = context;
        }

        // GET: api/workorders/active-progresses
        // Возвращает прогресс всех активных заказов (без перезагрузки страницы)
        [HttpGet("active-progresses")]
        public async Task<IActionResult> GetActiveProgresses()
        {
            await _productionService.UpdateAllProgresses();

            var activeOrders = await _context.WorkOrders
                .Where(w => w.Status == "InProgress")
                .Select(w => new
                {
                    w.Id,
                    w.ProgressPercent,
                    w.Status,
                    w.EstimatedEndDate,
                    w.ActualEndDate
                })
                .ToListAsync();

            return Ok(activeOrders);
        }
    }
}