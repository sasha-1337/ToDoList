using Azure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ToDoApp.DTOs.ToDoDTOs;
using ToDoApp.Services;

namespace ToDoApp.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TaskItemsController : Controller
    {
        private readonly ITaskItemService _taskItemService;

        public TaskItemsController(ITaskItemService taskItemService)
        {
            _taskItemService = taskItemService;
        }

        private int GetCurrentUserId()
        {
            // Перевіряємо за стандартом JWT "sub" (Subject)
            var claim = User.FindFirst(JwtRegisteredClaimNames.Sub)
                        ?? User.FindFirst(ClaimTypes.NameIdentifier)
                        ?? User.FindFirst("sub");

            if (claim == null || !int.TryParse(claim.Value, out var id))
            {
                throw new UnauthorizedAccessException("User ID is missing or invalid in JWT claims.");
            }

            return id;
        }

        [HttpGet] // GET: api/tasks? cursor = 2024 - 01 - 01T12:00:00&pageSize=10&categoryId=1
        [ProducesResponseType(typeof(PagedResponseDto<TaskItemResponseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResponseDto<TaskItemResponseDto>>> GetAll(
            [FromQuery] string? cursor,
            [FromQuery] int pageSize = 10,
            [FromQuery] int? categoryId = null,
            [FromQuery] string? searchQuery = null,
            [FromQuery] string? sortDirection = "desc",
            [FromQuery] bool? isCompleted = null)
        {
            var response = await _taskItemService.GetAsync(GetCurrentUserId(), cursor, pageSize, categoryId, searchQuery,sortDirection , isCompleted);
            return Ok(response);
        }

        [HttpGet("{id}")]  // GET: api/tasks/5
        [ProducesResponseType(typeof(TaskItemResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<TaskItemResponseDto>> GetById(int id)
        {
            var task = await _taskItemService.GetByIdAsync(id, GetCurrentUserId());

            return task!= null ? Ok(task) : NotFound(new { message = "Task isn't found or you don't have permission" });
        }

        [HttpPost] // POST: api/tasks
        [ProducesResponseType(typeof(TaskItemResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<TaskItemResponseDto>> Create([FromBody] TaskItemCreateUpdateDto taskDto)
        {
            var createdTask = await _taskItemService.CreateAsync(taskDto, GetCurrentUserId());
            return CreatedAtAction(nameof(GetById), new { id = createdTask.Id }, createdTask);
        }

        [HttpPut("{id}")] // PUT: api/tasks/5
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] TaskItemCreateUpdateDto taskDto)
        {
            var success = await _taskItemService.UpdateAsync(id, taskDto, GetCurrentUserId());
            return success ? NoContent() : NotFound(new { message = "Task isn't found or you don't have permission" });
        }

        [HttpPatch("{id}/status")] // PATCH: api/tasks/5/status
        [ProducesResponseType(typeof(UpdateTaskStatusResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UpdateTaskStatusResponseDto>> ToggleStatus(int id)
        {
            var result = await _taskItemService.ToggleStatusAsync(id, GetCurrentUserId());

            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }

        [HttpDelete("{id}")] // DELETE: api/tasks/5
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _taskItemService.DeleteAsync(id, GetCurrentUserId());
            return success ? NoContent() : NotFound(new { message = "Task isn't found or you don't have permission" });
        }


        [HttpPost("bulk-move")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> BulkMoveToCategory([FromBody] BulkMoveRequestDto dto)
        {
            if (dto.TaskIds == null || !dto.TaskIds.Any()) return BadRequest("No tasks selected.");

            await _taskItemService.BulkMoveAsync(dto.TaskIds, dto.CategoryId, GetCurrentUserId());
            return Ok();
        }

        [HttpPost("bulk-delete")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> BulkDelete([FromBody] BulkDeleteRequestDto dto)
        {
            if (dto.TaskIds == null || !dto.TaskIds.Any()) return BadRequest("No tasks selected.");

            await _taskItemService.BulkDeleteAsync(dto.TaskIds, GetCurrentUserId());
            return Ok();
        }

        [HttpPatch("bulk-status")]
        [ProducesResponseType(typeof(BulkUpdateStatusResponseDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<BulkUpdateStatusResponseDto>> BulkUpdateStatus([FromBody] BulkUpdateStatusRequestDto dto)
        {
            if (dto.TaskIds == null || !dto.TaskIds.Any()) return BadRequest("No tasks selected.");

            // Отримуємо новий загальний рахунок користувача після масового оновлення
            var response = await _taskItemService.BulkUpdateStatusAsync(dto.TaskIds, dto.IsCompleted, GetCurrentUserId());

            return Ok(response);
        }
    }
}
