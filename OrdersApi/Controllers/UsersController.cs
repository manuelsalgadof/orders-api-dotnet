using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrdersApi.DTOs;
using OrdersApi.Exceptions;
using OrdersApi.Interfaces;
using System.Security.Claims;

namespace OrdersApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
        {
            try
            {
                var created = await _userService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (DuplicateUserException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _userService.GetPagedAsync(page, pageSize);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var user = await _userService.GetByIdAsync(id);
            return user is null ? NotFound(new { message = "Usuario no encontrado." }) : Ok(user);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateUserDto dto)
        {
            try
            {
                var updated = await _userService.UpdateAsync(id, dto);
                return Ok(updated);
            }
            catch (DuplicateUserException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!TryGetRequestingUserId(out var requestingUserId))
                return Unauthorized(new { message = "Token inválido." });

            try
            {
                await _userService.DeleteAsync(id, requestingUserId);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        private bool TryGetRequestingUserId(out int userId)
        {
            userId = 0;
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(claim)) return false;
            if (!int.TryParse(claim, out var parsed) || parsed <= 0) return false;
            userId = parsed;
            return true;
        }
    }
}
