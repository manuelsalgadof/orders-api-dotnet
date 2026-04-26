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
        public async Task<IActionResult> Create(CreateOrderDto dto)
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
        public async Task<IActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _service.GetPagedAsync(page, pageSize);
            return Ok(result);
        }
    }
}