using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrdersApi.DTOs;
using OrdersApi.Exceptions;
using OrdersApi.Interfaces;

namespace OrdersApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _service;

        public OrdersController(IOrderService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateOrderDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _service.CreateAsync(dto);
                return CreatedAtAction(nameof(Create), new { id = result.Id }, result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (DuplicateOrderException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
        {
            var result = await _service.GetPagedAsync(page, pageSize);
            return Ok(result);
        }

        [HttpGet("export")]
        [Authorize(Policy = "OperatorUp")]
        public async Task<IActionResult> Export(CancellationToken cancellationToken = default)
        {
            var csv = await _service.ExportCsvAsync(cancellationToken);
            return File(
                System.Text.Encoding.UTF8.GetBytes(csv),
                "text/csv",
                $"orders-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv"
            );
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken = default)
        {
            var result = await _service.GetByIdAsync(id, cancellationToken);
            return result is null
                ? NotFound(new { message = "Pedido no encontrado." })
                : Ok(result);
        }
    }
}