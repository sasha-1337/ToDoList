using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Query;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ToDoApp.DTOs.ToDoDTOs;
using ToDoApp.Services;

namespace ToDoApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CategoriesController : Controller
    {
        private readonly ICategoryService _categoryService;

        public CategoriesController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
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

        [HttpGet] // GET: api/categories
        public async Task<ActionResult<IEnumerable<CategoryResponseDto>>> GetAll()
        {

            int userId = GetCurrentUserId();
            var categories = await _categoryService.GetAllCategoriesAsync(userId);
            return Ok(categories);

        }

        [HttpGet("{id}")] // GET: api/categories/{id})
        public async Task<ActionResult<CategoryResponseDto>> GetById(int id)
        {

            int userId = GetCurrentUserId();
            var category = await _categoryService.GetCategoryByIdAsync(id, userId);
            if (category == null)
            {
                return NotFound(new { message = "Category isn't found or you don't have permission" });
            }
            return Ok(category);

        }

        [HttpPost] // POST: api/categories
        public async Task<ActionResult<CategoryResponseDto>> Create([FromBody] CategoryCreateUpdateDto categoryDto)
        {

            int userId = GetCurrentUserId();
            var createdCategory = await _categoryService.CreateCategoryAsync(categoryDto, userId);
            return CreatedAtAction(nameof(GetById), new { id = createdCategory.Id }, createdCategory);

        }

        [HttpPut("{id}")] // PUT: api/categories/{id}
        public async Task<IActionResult> Update(int id, [FromBody] CategoryCreateUpdateDto categoryDto)
        {
            int userId = GetCurrentUserId();
            var updated = await _categoryService.UpdateCategoryAsync(id, categoryDto, userId);
            if (!updated)
            {
                return NotFound(new { message = "Category not found" });
            }
            return NoContent();
        }

        [HttpDelete("{id}")] // DELETE: api/categories/{id}
        public async Task<IActionResult> Delete(int id)
        {
            int userId = GetCurrentUserId();
            var deleted = await _categoryService.DeleteCategoryAsync(id, userId);
            if (!deleted)
            {
                return NotFound(new { message = "Category not found" });
            }
            return NoContent();
        }
    }
}
