using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrdersApi.Interfaces;

namespace OrdersApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class JobsController : ControllerBase
    {
        private readonly IJobService _service;

        public JobsController(IJobService service)
        {
            _service = service;
        }

        [HttpPost("reprocess-orders")]
        public async Task<IActionResult> ReprocessOrders()
        {
            var result = await _service.ReprocessOrdersAsync();

            return AcceptedAtAction(
                nameof(GetById),
                new { id = result.Id },
                result
            );
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _service.GetByIdAsync(id);

            if (result == null)
                return NotFound(new { message = "Job no encontrado." });

            return Ok(result);
        }
    }
}