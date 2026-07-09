using ToDoApp.Data;
using ToDoApp.Models;
using ToDoApp.DTOs.ToDoDTOs;
using Microsoft.EntityFrameworkCore;

namespace ToDoApp.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ToDoAppDbContext _dbContext;
        private readonly ILogger<CategoryService> _logger;

        public CategoryService(ToDoAppDbContext dbContext, ILogger<CategoryService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        async Task<IEnumerable<CategoryResponseDto>> ICategoryService.GetAllCategoriesAsync(int userId)
        {
            try
            {
                return await _dbContext.Categories
                    .Where(c => c.UserId == userId)
                    .Select(c => new CategoryResponseDto
                    {
                        Id = c.Id,
                        Name = c.Name
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving categories for user with ID {UserId}", userId);
                throw;
            }
        }

        async Task<CategoryResponseDto?> ICategoryService.GetCategoryByIdAsync(int categoryId, int userId)
        {
            try
            {
                return await _dbContext.Categories
                    .Where(c => c.Id == categoryId && c.UserId == userId)
                    .Select(c => new CategoryResponseDto
                    {
                        Id = c.Id,
                        Name = c.Name
                    })
                    .FirstOrDefaultAsync();

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving category with ID {CategoryId} for user with ID {UserId}", categoryId, userId);
                throw;
            }
        }

        async Task<CategoryResponseDto> ICategoryService.CreateCategoryAsync(CategoryCreateUpdateDto categoryDto, int userId)
        {
            try
            {
                var category = new Category
                {
                    Name = categoryDto.Name,
                    UserId = userId
                };

                _dbContext.Categories.Add(category);
                _dbContext.SaveChanges();

                _logger.LogInformation("Category with ID {CategoryId} created for user with ID {UserId}", category.Id, userId);

                return new CategoryResponseDto
                {
                    Id = category.Id,
                    Name = category.Name
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating a new category for user with ID {UserId}", userId);
                throw;
            }
        }

        async Task<bool> ICategoryService.UpdateCategoryAsync(int categoryId, CategoryCreateUpdateDto categoryDto, int userId)
        {
            try
            {
                var category = await _dbContext.Categories.FirstOrDefaultAsync(c => c.Id == categoryId && c.UserId == userId);
                if (category == null)
                {
                    _logger.LogWarning("Category with ID {CategoryId} not found for user with ID {UserId}", categoryId, userId);
                    return false;
                }

                category.Name = categoryDto.Name;
                _dbContext.Categories.Update(category);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Category with ID {CategoryId} updated for user with ID {UserId}", categoryId, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating category with ID {CategoryId} for user with ID {UserId}", categoryId, userId);
                throw;
            }
        }

        async Task<bool> ICategoryService.DeleteCategoryAsync(int categoryId, int userId)
        {
            try
            {
                var category = await _dbContext.Categories.FirstOrDefaultAsync(c => c.Id == categoryId && c.UserId == userId);
                if (category == null)
                {
                    _logger.LogWarning("Category with ID {CategoryId} not found for user with ID {UserId}", categoryId, userId);
                    return false;
                }
                _dbContext.Categories.Remove(category);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Category with ID {CategoryId} deleted for user with ID {UserId}", categoryId, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while deleting category with ID {CategoryId} for user with ID {UserId}", categoryId, userId);
                throw;
            }
        }
    }
}
